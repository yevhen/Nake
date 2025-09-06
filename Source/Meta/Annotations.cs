using System;

namespace Nake;

[AttributeUsage(AttributeTargets.Method)]
public class NakeAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class StepAttribute : Attribute;
