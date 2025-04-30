// -------------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------------

using UnityEngine;
using ZebugProject;

namespace ZebugTest
{

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
        public GraphAxisDefaultsDebug() : base(nameof(GraphAxisDefaultsDebug), Color.red, Zebug.Instance)
        {
            SetGraphValueMinMax(0, 2);
        }
    }

    protected void Update()
    {
        Zebug.GraphValue(Mathf.Sin(Time.time % 2f));

        GraphAxisDefaultsDebug.GraphValue(Mathf.Sin(Time.time * 15));
    }

}

}