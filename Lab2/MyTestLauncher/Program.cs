using System.Reflection;
using MyTestLauncher;

var testAssembly = Assembly.LoadFrom("MyTestProject.dll");
var runner = new TestLauncher();
runner.LaunchTest(testAssembly);