using System.Collections.Generic;
using MysticCircus.Editor;
using NUnit.Framework;

namespace MysticCircus.Tests
{
    public sealed class StoryGraphTests
    {
        const string Sample = @"{
            ""start"": ""a"",
            ""nodes"": [
                { ""id"": ""a"", ""type"": ""film"", ""video"": ""a.mp4"",
                  ""choices"": [ { ""zh"": ""左"", ""en"": ""Left"", ""next"": ""b"", ""setFlag"": ""left"" } ] },
                { ""id"": ""b"", ""type"": ""level"", ""scene"": ""S"", ""next"": ""c"",
                  ""routes"": [ { ""ifFlag"": ""left"", ""next"": ""d"" } ] },
                { ""id"": ""c"", ""type"": ""film"", ""video"": ""c.mp4"" },
                { ""id"": ""d"", ""type"": ""film"", ""video"": ""d.mp4"" }
            ]
        }";

        [Test]
        public void Json_ParsesNodesChoicesAndOmittedLists()
        {
            var graph = StoryGraph.FromJson(Sample);
            Assert.IsEmpty(graph.Validate());
            Assert.AreEqual("left", graph.Find("a").choices[0].setFlag);
            Assert.IsTrue(graph.Find("a").HasChoices);
            Assert.IsFalse(graph.Find("c").HasChoices);
            Assert.AreEqual("b", graph.FindLevel("S").id);
            Assert.IsNull(graph.Find("nope"));
        }

        [Test]
        public void ResolveNext_FirstSetFlagWinsOtherwiseNext()
        {
            var node = StoryGraph.FromJson(Sample).Find("b");
            Assert.AreEqual("c", StoryGraph.ResolveNext(node, _ => false));
            Assert.AreEqual("d", StoryGraph.ResolveNext(node, flag => flag == "left"));
        }

        [TestCase(@"{""start"":""x"",""nodes"":[{""id"":""a"",""type"":""level"",""scene"":""S""}]}", "start")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""level"",""scene"":""S""},{""id"":""a"",""type"":""level"",""scene"":""S""}]}", "Duplicate")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""level"",""scene"":""S"",""next"":""x""}]}", "unknown")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""movie""}]}", "type")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""level""}]}", "scene")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""film""}]}", "video")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""film"",""video"":""v"",""choices"":[{""zh"":""z""}]}]}", "no next")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""film"",""video"":""v"",""defaultChoice"":3,""choices"":[{""next"":""a""}]}]}", "defaultChoice")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""level"",""scene"":""S"",""choices"":[{""next"":""a""}]}]}", "only work on films")]
        public void Validate_ReportsContentMistakes(string json, string expected)
        {
            List<string> problems = StoryGraph.FromJson(json).Validate();
            Assert.IsTrue(problems.Exists(p => p.Contains(expected)), string.Join("; ", problems));
        }

        // Fails when someone edits story.json into a state the game cannot play in full.
        [Test]
        public void ShippedStory_HasEveryLevelAndFilmItNames()
        {
            Assert.IsEmpty(StoryTools.Check());
        }
    }
}
