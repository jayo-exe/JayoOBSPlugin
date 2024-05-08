using System;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace JayoOBSPlugin
{
    class VNyanTriggerListener : MonoBehaviour
    {
        private string triggerPrefix;
        public event Action<string> TriggerFired;

        public void Awake()
        {

            string assemblyName = "TriggerSystem, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            
            if (Type.GetType(assemblyName) == null) return;

            //Find the first(only) event in the TriggerSystem and hook our handler into it
            EventInfo triggerEvent = Type.GetType(assemblyName).GetEvents(BindingFlags.Public | BindingFlags.Static).FirstOrDefault();
            MethodInfo handlerMethod = typeof(VNyanTriggerListener).GetMethod("HandleTrigger");
            Delegate triggerDelegate = Delegate.CreateDelegate(triggerEvent.EventHandlerType, this, handlerMethod);
            triggerEvent.AddEventHandler(null, triggerDelegate);
            
        } 

        public void HandleTrigger(object arg)
        {
            //If we're presented with a null org or haven't set a prefix, do nothing
            if (triggerPrefix == null) { return; }
            if (arg == null) { return; }

            //get the Value property from the arg (this is the trigger name)
            string triggerValue = (string)arg.GetType().GetProperty("Value").GetValue(arg);

            //If the trigger doesn't match our desired prefix, do nothing
            if (!triggerValue.StartsWith(triggerPrefix)) { return; }

            //Pass the trigger up as an event
            TriggerFired.Invoke(triggerValue);
        }
        public void Listen(string prefix)
        {
            triggerPrefix = prefix;
        }

        public void StopListen()
        {
            triggerPrefix = null;
        }


    }
}
