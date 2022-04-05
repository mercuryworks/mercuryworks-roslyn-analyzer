using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VerifyCS = orville_bailey_analyzers.Test.CSharpAnalyzerVerifier<orville_bailey_analyzers.DoNotUseDefaultValueForEnums>;

namespace orville_bailey_analyzers.Test
{
    [TestClass]
    public class DoNotUseDefaultValueForEnumsTests
    {
        [TestMethod]
        public async Task empty_string_has_no_issues()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task manually_set_enums_trigger()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace test
{
    public enum TestEnum
    {

        {|#0:abc = 0|}
    }
} ";

            var expected = VerifyCS.Diagnostic("DoNotUseDefaultValueForEnums").WithLocation(0).WithArguments("abc");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task default_enums_trigger()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace test
{
    public enum TestEnum
    {

        {|#0:abc|}
    }
} ";

            var expected = VerifyCS.Diagnostic("DoNotUseDefaultValueForEnums").WithLocation(0).WithArguments("abc");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}

namespace test
{
    public enum TestEnum
    {
        abc = 0
    }
}
