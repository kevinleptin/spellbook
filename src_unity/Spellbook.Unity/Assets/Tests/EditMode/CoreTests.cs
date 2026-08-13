using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Spellbook.Core;

namespace Spellbook.Tests
{
    /// <summary>核心逻辑测试:与 WPF 版测试套件对齐,外加两版 JSON 互通性验证。</summary>
    public class CoreTests
    {
        private string _dir;
        private string _file;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "SpellbookUnityTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _file = Path.Combine(_dir, "items.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        // ―― LaunchKind 分类(与 WPF 版 ScriptRunnerTests 一致) ――

        [TestCase("http://example.com", LaunchKind.Url)]
        [TestCase("https://example.com/page?a=1", LaunchKind.Url)]
        [TestCase("HTTPS://EXAMPLE.COM", LaunchKind.Url)]
        [TestCase(@"C:\s\a.ps1", LaunchKind.Script)]
        [TestCase(@"C:\s\a.PS1", LaunchKind.Script)]
        [TestCase(@"C:\Program Files\app\chrome.exe", LaunchKind.Program)]
        [TestCase(@"C:\data\report.xlsx", LaunchKind.Program)]
        public void GetLaunchKind_ClassifiesByPath(string path, LaunchKind expected)
            => Assert.AreEqual(expected, Launcher.GetLaunchKind(path));

        [Test]
        public void GetLaunchKind_ExistingDirectory_IsFolder()
            => Assert.AreEqual(LaunchKind.Folder, Launcher.GetLaunchKind(Path.GetTempPath()));

        [Test]
        public void TargetMissing_UrlNeverMissing()
            => Assert.IsFalse(Launcher.TargetMissing("https://example.com"));

        [Test]
        public void BuildPsArguments_QuotesPath_AppendsArgs()
        {
            Assert.AreEqual(
                "-ExecutionPolicy Bypass -File \"C:\\my scripts\\a.ps1\"",
                Launcher.BuildPsArguments(@"C:\my scripts\a.ps1", ""));
            Assert.AreEqual(
                "-ExecutionPolicy Bypass -File \"C:\\s\\a.ps1\" -a 1",
                Launcher.BuildPsArguments(@"C:\s\a.ps1", "-a 1"));
        }

        // ―― 与 WPF 版(System.Text.Json)的数据互通 ――

        [Test]
        public void Load_ReadsWpfWrittenJson()
        {
            // WPF 版 System.Text.Json 缩进输出的真实样例(含中文与转义反斜杠)
            File.WriteAllText(_file, @"[
  {
    ""Name"": ""清理临时文件"",
    ""ScriptPath"": ""C:\\scripts\\清理.ps1"",
    ""Arguments"": ""-Days 7"",
    ""Notes"": ""每周跑一次"",
    ""GroupName"": ""运维"",
    ""SortOrder"": 0,
    ""IconKey"": ""fireball""
  }
]");
            var items = new ItemStore(_file).Load();
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("清理临时文件", items[0].Name);
            Assert.AreEqual(@"C:\scripts\清理.ps1", items[0].ScriptPath);
            Assert.AreEqual("运维", items[0].GroupName);
            Assert.AreEqual("fireball", items[0].IconKey);
        }

        [Test]
        public void Save_ProducesPascalCaseJson_WpfCanRead()
        {
            var store = new ItemStore(_file);
            store.Save(new System.Collections.Generic.List<SpellItem>
            {
                new SpellItem { Name = "测试", ScriptPath = @"C:\a.ps1", IconKey = "book" },
            });
            var json = File.ReadAllText(_file);
            StringAssert.Contains("\"Name\"", json);         // PascalCase 属性名
            StringAssert.Contains("\"ScriptPath\"", json);
            StringAssert.Contains("测试", json);              // 中文不转义
        }

        [Test]
        public void Load_CorruptJson_ReturnsEmpty_SetsLoadFailed_KeepsFile()
        {
            File.WriteAllText(_file, "{ not valid json !!!");
            var store = new ItemStore(_file);
            var items = store.Load();
            Assert.IsEmpty(items);
            Assert.IsTrue(store.LoadFailed);
            StringAssert.Contains("not valid", File.ReadAllText(_file));   // 原文件未被覆盖
        }

        [Test]
        public void Load_MissingFieldsInOldData_Defaulted()
        {
            // 旧版数据可能缺 IconKey 字段
            File.WriteAllText(_file,
                @"[{""Name"":""a"",""ScriptPath"":""C:\\a.ps1"",""Arguments"":"""",""Notes"":"""",""GroupName"":"""",""SortOrder"":0}]");
            var items = new ItemStore(_file).Load();
            Assert.AreEqual("", items[0].IconKey);
        }

        // ―― SpellbookModel 分组/排序逻辑(与 WPF 版 MainViewModelTests 对齐) ――

        private SpellbookModel NewModel() => new SpellbookModel(new ItemStore(_file));

        private static SpellItem Item(string name, string group = "") => new SpellItem
        {
            Name = name,
            ScriptPath = @"C:\scripts\" + name + ".ps1",
            GroupName = group,
        };

        [Test]
        public void Add_AppendsToGroupEnd_AndPersists()
        {
            var model = NewModel();
            model.Add(Item("a", "运维"));
            model.Add(Item("b", "运维"));

            var reloaded = NewModel();
            var group = reloaded.ItemsIn("运维");
            Assert.AreEqual(new[] { "a", "b" }, group.Select(i => i.Name).ToArray());
            Assert.Less(group[0].SortOrder, group[1].SortOrder);
        }

        [Test]
        public void GroupKeys_UngroupedFirst_ThenFirstAppearance()
        {
            var model = NewModel();
            model.Add(Item("z1", "Z组"));
            model.Add(Item("a1", "A组"));
            model.Add(Item("free"));

            Assert.AreEqual(new[] { "", "Z组", "A组" }, model.GroupKeys().ToArray());
        }

        [Test]
        public void MoveToGroup_AppendsToTargetEnd()
        {
            var model = NewModel();
            model.Add(Item("a", "G1"));
            model.Add(Item("b", "G2"));
            var a = model.ItemsIn("G1")[0];

            model.MoveToGroup(a, "G2");

            Assert.IsEmpty(model.ItemsIn("G1"));
            Assert.AreEqual(new[] { "b", "a" },
                model.ItemsIn("G2").Select(i => i.Name).ToArray());
        }

        [Test]
        public void ReorderBefore_MovesSourceBeforeTarget_Renumbers()
        {
            var model = NewModel();
            model.Add(Item("a", "G"));
            model.Add(Item("b", "G"));
            model.Add(Item("c", "G"));
            var items = model.ItemsIn("G");

            model.ReorderBefore(items[2], items[0]);   // c 插到 a 前

            Assert.AreEqual(new[] { "c", "a", "b" },
                model.ItemsIn("G").Select(i => i.Name).ToArray());
            Assert.AreEqual(new[] { 0, 1, 2 },
                model.ItemsIn("G").Select(i => i.SortOrder).ToArray());
        }

        [Test]
        public void ReorderBefore_DifferentGroups_NoOp()
        {
            var model = NewModel();
            model.Add(Item("a", "G1"));
            model.Add(Item("b", "G2"));

            model.ReorderBefore(model.ItemsIn("G1")[0], model.ItemsIn("G2")[0]);

            Assert.AreEqual(1, model.ItemsIn("G1").Count);
            Assert.AreEqual(1, model.ItemsIn("G2").Count);
        }

        [Test]
        public void Matches_CaseInsensitive_EmptyMatchesAll()
        {
            var item = Item("Cleanup");
            Assert.IsTrue(SpellbookModel.Matches(item, ""));
            Assert.IsTrue(SpellbookModel.Matches(item, "clean"));
            Assert.IsTrue(SpellbookModel.Matches(item, "CLEAN"));
            Assert.IsFalse(SpellbookModel.Matches(item, "xyz"));
        }
    }
}
