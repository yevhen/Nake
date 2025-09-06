using System;

namespace Nake;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class NakeAttribute : Attribute
{

}    

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class StepAttribute : Attribute
{

}
