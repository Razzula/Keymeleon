using Keymeleon;

namespace KeymeleonTests
{
    public class ConfigManagerTests
    {
        private ConfigManager configManager;

        [SetUp]
        public void Setup()
        {
            configManager = new ConfigManager();
            Directory.CreateDirectory("data/output/");
        }

        // FILE HANDLING --------

        [Test]
        public void LoadBaseConfig_FileDoesNotExist_ShouldReturnNull()
        {
            if (File.Exists("FakeFile")) { File.Delete("FakeFile"); }

            Dictionary<string, int[]> result = configManager.LoadBaseConfig("FakeFile");
            Assert.That(result, Is.EqualTo(null));
        }

        [Test]
        public void LoadBaseConfig_ValidFile_ShouldUpdateBaseLayer()
        {
            if (!File.Exists("data/input/esc_ffffff.base")) { Assert.Ignore("data/input/esc_ffffff.base does not exist"); }

            Dictionary<string, int[]> result = configManager.LoadBaseConfig("data/input/esc_ffffff.base");
            Assert.That(result, Has.Member(new KeyValuePair<string, int[]>("Esc", new int[] { 255, 255, 255 })));
        }

        [Test]
        public void LoadBaseConfig_InvalidFile_ShouldNotAffectBaseLayer()
        {
            if (!File.Exists("data/input/invalid")) { Assert.Ignore("data/input/invalid does not exist"); }

            Dictionary<string, int[]> result = configManager.LoadBaseConfig("data/input/invalid");
            foreach (var item in result)
            {
                if (!item.Value.SequenceEqual(new int[] {0,0,0}))
                {
                    Assert.Fail($"baseLayer value has changed ({item.Key} is now {item.Value})");
                }
            }
        }

        [Test]
        public void LoadLayerConfig_FileDoesNotExist_ShouldReturnNull()
        {
            if (File.Exists("FakeFile")) { File.Delete("FakeFile"); }

            Dictionary<string, int[]> result = configManager.LoadLayerConfig("FakeFile", 1);
            Assert.That(result, Is.EqualTo(null));
        }

        [Test]
        public void LoadLayerConfig_ValidFile_ShouldUpdateLayer()
        {
            if (!File.Exists("data/input/esc_ffffff.layer")) { Assert.Ignore("data/input/esc_ffffff.layer does not exist"); }

            Dictionary<string, int[]> result = configManager.LoadLayerConfig("data/input/esc_ffffff.layer", 1);
            Assert.That(result, Has.Member(new KeyValuePair<string, int[]>("Esc", new int[] { 255, 255, 255 })));
        }

        [Test]
        public void LoadLayerConfig_InvalidFile_ShouldNotAffectLayer()
        {
            if (!File.Exists("data/input/invalid")) { Assert.Ignore("data/input/invalid does not exist"); }

            Dictionary<string, int[]> result = configManager.LoadLayerConfig("data/input/invalid", 1);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void LoadLayerConfig_ExistingLayer_ShouldOverwrite()
        {
            if (!File.Exists("data/input/esc_ffffff.layer")) { Assert.Ignore("data/input/esc_ffffff.layer does not exist"); }

            configManager.UpdateLayer(1, "F1", 255, 0, 0);
            Dictionary<string, int[]> result = configManager.LoadLayerConfig("data/input/esc_ffffff.layer", 1);
            Assert.That(result, Has.No.Member(new KeyValuePair<string, int[]>("F1", new int[] { 255, 0, 0 })));
        }

        [Test]
        public void SaveBaseConfig_FileDoesNotExist_ShouldCreateFile()
        {
            if (File.Exists("data/output/test.base")) { File.Delete("data/output/test.base"); }

            configManager.SaveBaseConfig("data/output/test.base");
            Assert.IsTrue(File.Exists("data/output/test.base"));
        }

        [Test]
        public void SaveBaseConfig_NonDefaultBaseValue_ShouldFormatCorrectly()
        {
            if (File.Exists("data/output/test.base")) { File.Delete("data/output/test.base"); }

            configManager.UpdateLayer(0, "Esc", 255, 255, 255);
            configManager.SaveBaseConfig("data/output/test.base");

            if (!File.Exists("data/expected/esc_ffffff.base")) { Assert.Ignore("data/expected/esc_ffffff.base does not exist"); }
            var expectedResult = File.ReadAllText("data/expected/esc_ffffff.base");
            var result = File.ReadAllText("data/output/test.base");
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void SaveLayerConfig_FileDoesNotExist_ShouldCreateFile()
        {
            if (File.Exists("data/output/test.layer")) { File.Delete("data/output/test.layer"); }

            configManager.SaveLayerConfig("data/output/test.layer", 1);
            Assert.IsTrue(File.Exists("data/output/test.layer"));
        }

        [Test]
        public void SaveLayerConfig_NonDefaultBaseValue_ShouldFormatCorrectly()
        {
            if (File.Exists("data/output/test.layer")) { File.Delete("data/output/test.layer"); }

            configManager.UpdateLayer(1, "Esc", 255, 255, 255);
            configManager.SaveLayerConfig("data/output/test.layer", 1);

            if (!File.Exists("data/expected/esc_ffffff.layer")) { Assert.Ignore("data/expected/esc_ffffff.layer does not exist"); }
            var expectedResult = File.ReadAllText("data/expected/esc_ffffff.layer");
            var result = File.ReadAllText("data/output/test.layer");
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        // LOGIC ---------

        // GetLayer
        [Test]
        public void GetLayer_BaaseDefaultState_ShouldReturnDefaultList()
        {
            var result = configManager.GetLayer(0);
            foreach (var item in result)
            {
                if (!item.Value.SequenceEqual(new int[] {0, 0, 0}))
                {
                    Assert.Fail($"Value of {item.Key} is {item.Value}");
                }
            }
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        public void GetLayer_LayersDefaultState_ShouldReturnEmptyList(int layer)
        {
            var result = configManager.GetLayer(layer);
            Assert.That(result, Is.Empty);
        }

        // Set
        [Test]
        [TestCase("Esc", new int[] { 1, 1, 1 })]
        [TestCase("Esc", new int[] { 255, 255, 255 })]
        public void SetBaseLayer_ValidValueForExistingKey_ShouldUpateBaseLayer(string key, int[] colour)
        {
            var defaultBase = configManager.GetLayer(0);
            int expectedLength = defaultBase.Count(); //store, as defaultBase will change upon calling SetBaseConfig

            var inputData = new Dictionary<string, int[]>();
            inputData.Add(key, colour);

            configManager.SetBaseConfig(inputData);

            var result = configManager.GetLayer(0);
            if (result.Count != expectedLength)
            {
                Assert.Fail("Length of list has changed");
            }
            Assert.That(result, Has.Member(new KeyValuePair<string, int[]>(key, colour)));
        }

        [Test]
        public void SetBaseLayer_ValidValueForNewKey_ShouldUpateBaseLayer()
        {
            var defaultBase = configManager.GetLayer(0);
            int expectedLength = defaultBase.Count(); //store, as defaultBase will change upon calling SetBaseConfig

            var inputData = new Dictionary<string, int[]>();
            inputData.Add("NewKey", new int[] { 1, 1, 1 });

            configManager.SetBaseConfig(inputData);

            var result = configManager.GetLayer(0);
            if (result.Count == expectedLength)
            {
                Assert.Fail($"Length of list has not changed (expected {expectedLength + 1} got {result.Count})");
            }
            Assert.That(result, Has.Member(new KeyValuePair<string, int[]>("NewKey", new int[] { 1, 1, 1 })));
        }

        //Update
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void UpdateLayer_ValidValues_ShouldUpdateLayer(int layer)
        {
            configManager.UpdateLayer(layer, "Esc", 255, 255, 255);
            var result = configManager.GetLayer(layer);

            Assert.That(result, Has.Member(new KeyValuePair<string, int[]>("Esc", new int[] { 255, 255, 255 })));
        }

        [Test]
        public void UpdateLayerMass_BlankLayer_ShouldFillAllKeys()
        {
            configManager.UpdateLayerMass("Esc", 255, 255, 255);
            var result = configManager.GetLayer(1);

            int[] expectedResult = { 255, 255, 255 };
            foreach (var item in result)
            {
                if (!item.Value.SequenceEqual(expectedResult))
                {
                    Assert.Fail($"{item.Key} not white");
                }
            }
            Assert.Pass();
        }

        [Test]
        public void UpdateLayerMass_NonBlankLayer_ShouldFillCertainKeys()
        {
            string[] toRecolour = { "Esc", "F1", "F3", "D", "F", "G" };
            foreach (var key in toRecolour)
            {
                configManager.UpdateLayer(1, key, 1, 1, 1);
            }
            configManager.UpdateLayerMass(toRecolour[0], 255, 255, 255);

            var result = configManager.GetLayer(1);

            int[] defaultBehaviour = { 255, 255, 255 };
            int[] expectedBehaviour = { 255, 255, 255 };
            //check result
            foreach (var item in result)
            {
                if (toRecolour.Contains(item.Key)) //should have been recoloured
                {
                    if (!item.Value.SequenceEqual(expectedBehaviour))
                    {
                        Assert.Fail($"{item.Key} not white");
                    }
                }
                else
                {
                    if (!item.Value.SequenceEqual(defaultBehaviour))
                    {
                        Assert.Fail($"{item.Key} not black");
                    }
                }
            }

            //check return
            foreach (var item in result)
            {
                if (!toRecolour.Contains(item.Key) || !item.Value.SequenceEqual(expectedBehaviour))
                {
                    Assert.Fail($"Invalid return ({item.Key}, {item.Value})");
                }
            }
            Assert.Pass();
        }

        [Test]
        public void UpdateLayerMass_EightKeys_ShouldCreateTempFile()
        {
            if (File.Exists("layouts/_temp.base")) { File.Delete("layouts/_temp.base"); }

            string[] toRecolour = { "1", "2", "3", "4", "5", "6", "7", "8" };
            foreach (var key in toRecolour)
            {
                configManager.UpdateLayer(1, key, 1, 1, 1);
            }
            configManager.UpdateLayerMass(toRecolour[0], 255, 255, 255);

            Assert.True(File.Exists("layouts/_temp.base"));
        }

        [Test]
        public void UpdateLayerMass_SevenKeys_ShouldNotCreateTempFile()
        {
            if (File.Exists("layouts/_temp.base")) { File.Delete("layouts/_temp.base"); }

            string[] toRecolour = { "1", "2", "3", "4", "5", "6", "7" };
            foreach (var key in toRecolour)
            {
                configManager.UpdateLayer(1, key, 1, 1, 1);
            }
            configManager.UpdateLayerMass(toRecolour[0], 255, 255, 255);

            Assert.True(!File.Exists("layouts/_temp.base"));
        }

        //Remove
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RemoveKey_BlankKey_ShouldReturnBlack(int layer)
        {

            var result = configManager.RemoveKey("Esc", layer);
            Assert.That(result, Is.EquivalentTo(new int[] {0, 0, 0}));
        }

        [Test]
        public void RemoveKey_LayerColour_ShouldReturnBase()
        {
            configManager.UpdateLayer(0, "Esc", 255, 255, 255);
            configManager.UpdateLayer(1, "Esc", 1, 1, 1);
            var result = configManager.RemoveKey("Esc", 1);
            Assert.That(result, Is.EquivalentTo(new int[] { 255, 255, 255 }));
        }

        [Test]
        public void RemoveKey_TopColourLayerColour_ShouldReturnLayer()
        {
            configManager.UpdateLayer(0, "Esc", 255, 0, 0);
            configManager.UpdateLayer(1, "Esc", 0, 255, 0);
            configManager.UpdateLayer(2, "Esc", 0, 0, 255);
            var result = configManager.RemoveKey("Esc", 2);
            Assert.That(result, Is.EquivalentTo(new int[] { 0, 255, 0 }));
        }

        [Test]
        public void RemoveKey_TopColourNoLayerColour_ShouldReturnBase()
        {
            configManager.UpdateLayer(0, "Esc", 255, 0, 0);
            configManager.UpdateLayer(2, "Esc", 0, 0, 255);
            var result = configManager.RemoveKey("Esc", 2);
            Assert.That(result, Is.EquivalentTo(new int[] { 255, 0, 0 }));
        }

        //Deltas
        [Test]
        [TestCase(0, 1, new int[] { 255, 0, 0 })]
        [TestCase(1, 2, new int[] { 0, 255, 0 })]
        public void GetStatesDelta_AdjacentLayersWithValues_ShouldReturnDelta(int bottom, int top, int[] expectedDelta)
        {
            configManager.UpdateLayer(0, "Esc", 255, 0, 0);
            configManager.UpdateLayer(1, "Esc", 0, 255, 0);
            configManager.UpdateLayer(2, "Esc", 0, 0, 255);

            var result = configManager.GetStatesDelta(bottom, top);
            Assert.That(result, Has.Member(new KeyValuePair<string, int[]>("Esc", expectedDelta)));
        }

        [Test]
        public void GetStatesDelta_NonAdjacent_ShouldReturnDelta()
        {
            configManager.UpdateLayer(0, "Esc", 255, 0, 0);
            configManager.UpdateLayer(2, "Esc", 0, 0, 255);

            var result = configManager.GetStatesDelta(0, 2);
            Assert.That(result, Has.Member(new KeyValuePair<string, int[]>("Esc", new int[] {255, 0, 0})));
        }
    }
}