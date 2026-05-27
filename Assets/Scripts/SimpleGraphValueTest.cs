// -------------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------------

using UnityEngine;
using ZebugProject;

namespace ZebugTest
{

[ExecuteInEditMode]
public class SimpleGraphValueTest : MonoBehaviour
{
    private class Zebug : Channel<Zebug>
    {
        public Zebug() : base(nameof(SimpleGraphValueTest), Color.red)
        {

        }
    }

    private class GraphAxisDefaultsDebug : Channel<GraphAxisDefaultsDebug>
    {
        public GraphAxisDefaultsDebug() : base(nameof(GraphAxisDefaultsDebug), Color.red, ZebugProject.Zebug.Instance)
        {
            SetGraphGridLine(0, Color.grey);
            SetGraphGridLine(1, new Color(0f, 0.47f, 0.47f, 0.55f), true);
        }
    }
    
    
    private class SubgraphOnlyZebug : Channel<SubgraphOnlyZebug>
    {
        public SubgraphOnlyZebug() : base(nameof(SubgraphOnlyZebug), Color.red, SimpleGraphValueTest.Zebug.Instance)
        {
            SetGraphGridLine(0, Color.grey);
            SetGraphGridLine(1, new Color(0f, 0.47f, 0.47f, 0.55f));
            SetSubgraphLine("subgraph", new Color(0.56f, 0.55f, 1f));
        }
    }
    
    private class DoubleSubgraphOnly : Channel<DoubleSubgraphOnly>
    {
        public DoubleSubgraphOnly() : base(nameof(DoubleSubgraphOnly), new Color(1f, 0.59f, 0.25f), SimpleGraphValueTest.Zebug.Instance)
        {
            SetGraphGridLine(0, Color.grey);
            SetGraphGridLine(1, new Color(0f, 0.47f, 0.47f, 0.55f));
            SetSubgraphLine("subgraph0", new Color(0.28f, 1f, 0.55f));
            SetSubgraphLine("subgraph1", new Color(0.56f, 0.55f, 1f));
        }
    }
    
    private class TestManyChannels : Channel<TestManyChannels> {
        public TestManyChannels() : base(nameof(TestManyChannels), Color.cyan) {}
    }
    
    private class ManyChannel0 : Channel<ManyChannel0> { public ManyChannel0() : base(nameof(ManyChannel0), Color.cyan, TestManyChannels.Instance) {}}
    private class ManyChannel1 : Channel<ManyChannel1> { public ManyChannel1() : base(nameof(ManyChannel1), Color.cyan, TestManyChannels.Instance) {}}
    private class ManyChannel2 : Channel<ManyChannel2> { public ManyChannel2() : base(nameof(ManyChannel2), Color.cyan, TestManyChannels.Instance) {}}
    private class ManyChannel3 : Channel<ManyChannel3> { public ManyChannel3() : base(nameof(ManyChannel3), Color.cyan, TestManyChannels.Instance) {}}
    private class ManyChannel4 : Channel<ManyChannel4> { public ManyChannel4() : base(nameof(ManyChannel4), Color.cyan, TestManyChannels.Instance) {}}
    private class ManyChannel5 : Channel<ManyChannel5> { public ManyChannel5() : base(nameof(ManyChannel5), Color.cyan, TestManyChannels.Instance) {}}
    private class ManyChannel6 : Channel<ManyChannel6> { public ManyChannel6() : base(nameof(ManyChannel6), Color.cyan, TestManyChannels.Instance) {}}
    private class ManyChannel7 : Channel<ManyChannel7> { public ManyChannel7() : base(nameof(ManyChannel7), Color.cyan, TestManyChannels.Instance) {}}
    private class ManyChannel8 : Channel<ManyChannel8> { public ManyChannel8() : base(nameof(ManyChannel8), Color.cyan, TestManyChannels.Instance) {}}
    private class ManyChannel9 : Channel<ManyChannel9> { public ManyChannel9() : base(nameof(ManyChannel9), Color.cyan, TestManyChannels.Instance) {}}

    
    private class VeryNested0 : Channel<VeryNested0> { public VeryNested0() : base(nameof(VeryNested0), Color.cyan) {}}
    private class VeryNested1 : Channel<VeryNested1> { public VeryNested1() : base(nameof(VeryNested1), Color.cyan, VeryNested0.Instance) {}}
    private class VeryNested2 : Channel<VeryNested2> { public VeryNested2() : base(nameof(VeryNested2), Color.cyan, VeryNested1.Instance) {}}
    private class VeryNested3 : Channel<VeryNested3> { public VeryNested3() : base(nameof(VeryNested3), Color.cyan, VeryNested2.Instance) {}}
    private class VeryNested4 : Channel<VeryNested4> { public VeryNested4() : base(nameof(VeryNested4), Color.cyan, VeryNested3.Instance) {}}
    private class VeryNested5 : Channel<VeryNested5> { public VeryNested5() : base(nameof(VeryNested5), Color.cyan, VeryNested4.Instance) {}}
    private class VeryNested6 : Channel<VeryNested6> { public VeryNested6() : base(nameof(VeryNested6), Color.cyan, VeryNested5.Instance) {}}
    private class VeryNested7 : Channel<VeryNested7> { public VeryNested7() : base(nameof(VeryNested7), Color.cyan, VeryNested6.Instance) {}}
    private class VeryNested8 : Channel<VeryNested8> { public VeryNested8() : base(nameof(VeryNested8), Color.cyan, VeryNested7.Instance) {}}
    private class VeryNested9 : Channel<VeryNested9> { public VeryNested9() : base(nameof(VeryNested9), Color.cyan, VeryNested8.Instance) {}}

    
    protected void Update()
    {
        Zebug.GraphValue(Mathf.Sin(Time.time % 2f));

        float value = Mathf.Sin(Time.time * 15);
        GraphAxisDefaultsDebug.GraphValue(value);
        
        float value2 = Mathf.Sin(Time.time * 7f) + 0.5f;
        float slowModulate = Mathf.Sin(Time.time * 0.125f);
        
        if (Time.frameCount % 100 != 0)
        {
            SubgraphOnlyZebug.GraphValue("subgraph", (value + value2) * slowModulate);
        }
        // else introduce a frame gap, for testing missing frame data
        
        DoubleSubgraphOnly.GraphValue("subgraph0", value * slowModulate);
        DoubleSubgraphOnly.GraphValue("subgraph1", (value + value2) * slowModulate);
    }

}

}