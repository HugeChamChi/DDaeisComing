using System;

namespace DDaeisComing.Attributes
{
    /// <summary>
    /// Attach to a method to expose it as a button in the Unity Inspector.
    /// Used as a replacement for ContextMenu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ButtonAttribute : Attribute
    {
        public string ButtonName { get; private set; }

        public ButtonAttribute()
        {
            ButtonName = null;
        }

        public ButtonAttribute(string buttonName)
        {
            ButtonName = buttonName;
        }
    }
}
