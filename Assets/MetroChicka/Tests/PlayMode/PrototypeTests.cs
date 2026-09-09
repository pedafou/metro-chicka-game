using System.Collections;
using System.Linq;
using MetroChicka.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MetroChicka.Tests
{
    public sealed class PrototypeTests
    {
        private Prototype game;
        [UnitySetUp] public IEnumerator Setup()
        {SceneManager.LoadScene("UnfoldStudy");yield return null;yield return null;game=Object.FindFirstObjectByType<Prototype>();Assert.That(game,Is.Not.Null);game.AnimationSpeed=12;}
        [UnityTest] public IEnumerator UiUnfoldShowsTrainThenDisembarksAndRemovesIt()
        {
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name=="Unfold").onClick.Invoke();
            bool trainSeen=false;float limit=Time.realtimeSinceStartup+5;
            while(game.Busy&&Time.realtimeSinceStartup<limit){trainSeen|=game.ActiveTrains>0;yield return null;}
            Assert.That(game.Busy,Is.False);Assert.That(trainSeen);yield return null;
            Assert.That(game.ActiveTrains,Is.Zero);Assert.That(game.Model.Remaining,Is.EqualTo(2));
            Assert.That(game.Model.Chickas[0].Position,Is.EqualTo(new Point(-1,-2)));
        }
        [UnityTest] public IEnumerator RelayUiAndUndoRestoreWholeChain()
        {
            game.LoadScenario(1);yield return null;Assert.That(game.CurrentPreview.Moves.Count,Is.EqualTo(3));Assert.That(game.Commit());
            float limit=Time.realtimeSinceStartup+5;while(game.Busy&&Time.realtimeSinceStartup<limit)yield return null;
            Assert.That(game.Busy,Is.False);Assert.That(game.Model.Remaining,Is.EqualTo(6));Assert.That(game.Model.Score,Is.EqualTo(60));
            game.Undo();Assert.That(game.Model.Remaining,Is.EqualTo(9));Assert.That(game.Model.Roads,Is.Empty);
            game.ToggleRelay();Assert.That(game.CurrentPreview.Moves.Count,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator BusyStateRejectsExtraActionsAndRestart()
        {
            Assert.That(game.Commit());Assert.That(game.Commit(),Is.False);game.LoadScenario(2);Assert.That(game.ScenarioIndex,Is.Zero);
            float limit=Time.realtimeSinceStartup+5;while(game.Busy&&Time.realtimeSinceStartup<limit)yield return null;Assert.That(game.Busy,Is.False);
        }
    }
}
