using System;

namespace Nake
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TaskAttribute : Attribute
    {

    }    
    
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class StepAttribute : Attribute
    {

    }
}
