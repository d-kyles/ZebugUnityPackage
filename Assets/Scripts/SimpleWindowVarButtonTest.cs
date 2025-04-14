using UnityEngine;
using ZebugProject;

public class SimpleWindowVarButtonTest : MonoBehaviour
{
    private class Zebug : Channel<Zebug>
    {
        public Zebug() : base("Window Var Button Test", new Color(0.45f, 0.25f, 0.824f))
        {
        }
        
    }
    
    protected void Start()
    {
        Zebug.AddWindowButton($"{name}Button", () =>
        {
            Zebug.Log($"{name} Button Pressed");
        });
    }
    
    protected void Update()
    {
        Zebug.LogToWindow($"{name}Var", $"{name} Var Value: {Time.time}");
    }
    
}
