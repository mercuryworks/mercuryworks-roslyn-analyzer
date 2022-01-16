using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = orville_bailey_analyzers.Test.CSharpCodeFixVerifier<
    orville_bailey_analyzers.DoNotReturnTuplesAnalyzer,
    orville_bailey_analyzers.DoNotReturnTuplesCodeFixProvider>;

namespace orville_bailey_analyzers.Test
{
    [TestClass]
    public class DoNotReturnTuplesUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task empty_string_has_no_issues()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task CustomClassesInTuple()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class TypeName
    {   
        public Tuple<ABC,int> {|#0:Test|}()
        {
            return Tuple.Create(new ABC(),1);
        }

        public class ABC
        {
            public ABC() 
            {
            }
        }
    }
}";

            var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class TypeName
    {   
        public TestDTO {|#0:Test|}()
        {
            return new TestDTO { Item1 = new ABC(), Item2 = 1 };
        }

        public class TestDTO
        {
            public ABC Item1 { get; set; }
            public int Item2 { get; set; }
        }

        public class ABC
        {
            public ABC() 
            {
            }
        }
    }
}";

            var expected = VerifyCS.Diagnostic("DoNotReturnTuples").WithLocation(0).WithArguments("Test");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task code_fix_creates_dto()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class TypeName
    {   
        public Tuple<int,int> {|#0:Test|}()
        {
            return Tuple.Create(1,1);
        }
    }
}";

            var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class TypeName
    {   
        public TestDTO Test()
        {
            return new TestDTO { Item1 = 1, Item2 = 1 };
        }

        public class TestDTO
        {
            public int Item1 { get; set; }
            public int Item2 { get; set; }
        }
    }
}";

            var expected = VerifyCS.Diagnostic("DoNotReturnTuples").WithLocation(0).WithArguments("Test");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task DiagnosticIsPresent()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public Tuple<int,int> {|#0:Test|}()
            {
                return Tuple.Create(1,1);
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("DoNotReturnTuples").WithLocation(0).WithArguments("Test");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task NamedTuplesTrigger()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class TypeName
    {   
        public (int a, int b) {|#0:Test|}()
        {
            return (1,3);
        }
    }
}";
            var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class TypeName
    {   
        public TestDTO Test()
        {
            return new TestDTO { Item1 = 1, Item2 = 3 };
        }

        public class TestDTO
        {
            public int Item1 { get; set; }
            public int Item2 { get; set; }
        }
    }
}";
        
            var expected = VerifyCS.Diagnostic("DoNotReturnTuples").WithLocation(0).WithArguments("Test");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

    }
}