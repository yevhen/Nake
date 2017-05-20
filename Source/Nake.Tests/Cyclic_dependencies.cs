﻿using System;

using NUnit.Framework;

namespace Nake.Tests
{
	[TestFixture]
	class Cyclic_dependencies : CodeFixture
	{
		[Test]
		public void Recursive_call()
		{
			Assert.Throws<RecursiveTaskCallException>(() => Build(@"
            
                [Step] void Step() 
                {
                    Step();
                }

            "));
		}

		[Test]
		public void Simplest_case_no_intermediaries()
		{
			Assert.Throws<CyclicDependencyException>(() => Build(@"
            
                [Step] void Step1() 
                {
                    Step2();
                }

                [Step] void Step2() 
                {
                    Step1();
                }

            "));
		}

		[Test]
		public void Via_intermediaries()
		{
			Assert.Throws<CyclicDependencyException>(() => Build(@"
            
                [Step] public static void Step1() 
                {
                    Step2();
                }

                [Step] public static void Step2() 
                {
                    Step3();
                }

                [Step] public static void Step3() 
                {
                    Step1();
                }

            "));
		}
	}
}