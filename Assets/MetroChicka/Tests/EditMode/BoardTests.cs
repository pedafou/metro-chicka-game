using System.Linq;
using MetroChicka.Core;
using NUnit.Framework;

namespace MetroChicka.Tests
{
    public sealed class BoardTests
    {
        private static Road Line(int ax,int ay,int bx,int by,Shell kind=Shell.Straight)=>new Road{Kind=kind,Main=new[]{new Point(ax,ay),new Point(bx,by)}};
        [Test] public void BasicUnfoldConsumesExactlyOneLayerAndLeavesRoad()
        {var b=Board.Scenario(0);var p=b.Preview(0,0);Assert.That(b.Apply(p));Assert.That(b.Remaining,Is.EqualTo(2));Assert.That(b.Roads.Count,Is.EqualTo(1));Assert.That(b.Chickas[0].Position,Is.EqualTo(new Point(-1,-2)));}
        [Test] public void RelayActivatesThreeLineagesOnceEach()
        {var b=Board.Scenario(1);var p=b.Preview(0,0);Assert.That(p.Moves.Select(m=>m.Id),Is.EqualTo(new[]{0,1,2}));Assert.That(p.Score,Is.EqualTo(60));b.Apply(p);Assert.That(b.Remaining,Is.EqualTo(6));}
        [Test] public void RelayOffConsumesOnlySelectedLineage()
        {var b=Board.Scenario(1);b.SetRelay(false);var p=b.Preview(0,0);Assert.That(p.Moves.Count,Is.EqualTo(1));Assert.That(p.Score,Is.EqualTo(10));b.Apply(p);Assert.That(b.Remaining,Is.EqualTo(8));}
        [Test] public void PreviewDoesNotConsumeResourcesOrCreateRoads()
        {var b=Board.Scenario(1);for(int i=0;i<100;i++)b.Preview(0,0);Assert.That(b.Remaining,Is.EqualTo(9));Assert.That(b.Roads,Is.Empty);Assert.That(b.Score,Is.Zero);}
        [Test] public void StalePreviewCannotApplyAfterAimChange()
        {var b=Board.Scenario(0);var p=b.Preview(0,0);b.Aim(0,1);Assert.That(b.Apply(p),Is.False);Assert.That(b.Remaining,Is.EqualTo(3));}
        [Test] public void StalePreviewCannotApplyAfterRelayChange()
        {var b=Board.Scenario(0);var p=b.Preview(0,0);b.SetRelay(false);Assert.That(b.Apply(p),Is.False);}
        [Test] public void APlanCannotBeSpentTwice()
        {var b=Board.Scenario(0);var p=b.Preview(0,0);Assert.That(b.Apply(p));Assert.That(b.Apply(p),Is.False);Assert.That(b.Remaining,Is.EqualTo(2));}
        [Test] public void OutOfBoundsMoveIsTransactional()
        {var b=Board.Scenario(0);var p=b.Preview(0,2);Assert.That(p.Error,Is.EqualTo("OUTSIDE"));Assert.That(b.Apply(p),Is.False);Assert.That(b.Actions,Is.Zero);Assert.That(b.Remaining,Is.EqualTo(3));}
        [Test] public void EmptyDollCannotUnfold()
        {var b=Board.Scenario(0);b.Chickas[0].Used=3;Assert.That(b.Preview(0,0).Error,Is.EqualTo("NO_LAYERS"));}
        [Test] public void ShellOrderChangesNextRoadGeometry()
        {var b=Board.Scenario(0);var first=Road.Create(b.Chickas[0],0);b.Chickas[0].Used=1;var second=Road.Create(b.Chickas[0],0);Assert.That(first.End,Is.Not.EqualTo(second.End));}
        [Test] public void CrossedOrdinaryRailsConnect()
        {Assert.That(Line(-2,0,2,0).Connects(Line(0,-2,0,2)));}
        [Test] public void BridgeInteriorDoesNotConductCrossingSignals()
        {Assert.That(Line(-2,0,2,0,Shell.Bridge).Connects(Line(0,-2,0,2)),Is.False);}
        [Test] public void BridgeEndpointDoesConduct()
        {Assert.That(Line(-2,0,2,0,Shell.Bridge).Connects(Line(2,0,2,3)));}
        [Test] public void DollUnderBridgeCannotBeTriggered()
        {Assert.That(Line(-2,0,2,0,Shell.Bridge).Touches(new Point(0,0)),Is.False);}
        [Test] public void DisconnectedCollinearRailsDoNotConnect()
        {Assert.That(Line(-4,0,-2,0).Connects(Line(0,0,2,0)),Is.False);}
        [Test] public void ForkSpurCarriesSignalButTrainEndsOnMain()
        {var b=Board.Scenario(2);var r=Road.Create(b.Chickas[0],0);Assert.That(r.Touches(new Point(-4,0)));Assert.That(r.End,Is.EqualTo(new Point(-2,-2)));}
        [Test] public void OldRoadCanReachDistantChicka()
        {var b=Board.Scenario(2);var p=b.Preview(0,0);Assert.That(p.Moves.Select(m=>m.Id),Does.Contain(2));}
        [Test] public void CyclesDoNotConsumeAdditionalLayers()
        {var b=Board.Scenario(1);b.Roads.Add(Line(-6,0,2,0));b.Roads.Add(Line(-6,0,2,0));var p=b.Preview(0,0);Assert.That(p.Moves.Count,Is.EqualTo(3));Assert.That(p.Moves.Select(m=>m.Id).Distinct().Count(),Is.EqualTo(3));}
        [Test] public void BlockedAutomaticUnfoldDoesNotSpendItsLayer()
        {var b=Board.Scenario(1);b.Aim(1,3);b.Chickas[1].Layers[0]=Shell.Bend;b.Chickas[1].Position=new Point(-2,-4);b.Roads.Add(Line(-2,0,-2,-4));var p=b.Preview(0,0);Assert.That(p.Blocked,Does.Contain(1));b.Apply(p);Assert.That(b.Chickas[1].Remaining,Is.EqualTo(3));}
        [Test] public void SnapshotIsIndependentAndCanRestoreWholeAction()
        {var b=Board.Scenario(1);var old=b.Copy();b.Apply(b.Preview(0,0));Assert.That(old.Remaining,Is.EqualTo(9));Assert.That(old.Roads,Is.Empty);Assert.That(old.Chickas[0].Position,Is.EqualTo(new Point(-6,0)));}
        [Test] public void AllFourDirectionsCanBeRotatedBack()
        {var p=new Point(2,3);Assert.That(Point.Rotate(p,4),Is.EqualTo(p));Assert.That(Point.Rotate(p,-1),Is.EqualTo(new Point(3,-2)));}
        [Test] public void RepeatedLegalActionsEventuallyExhaustFiniteLayers()
        {var b=Board.Scenario(1);int safety=0;while(b.Remaining>0&&safety++<20){bool moved=false;foreach(var c in b.Chickas){for(int d=0;d<4;d++){var p=b.Preview(c.Id,d);if(p.Error==null){Assert.That(b.Apply(p));moved=true;break;}}if(moved)break;}Assert.That(moved,"Every supplied scenario must have a legal continuation");}Assert.That(b.Remaining,Is.Zero);Assert.That(safety,Is.LessThanOrEqualTo(9));}
    }
}
