using Xunit;
using Hypar.Functions.Execution.Local;
using Xunit.Abstractions;
using System.Threading.Tasks;
using Hypar.Functions.Execution;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Elements.Serialization.glTF;
using System.Text;
using System;

namespace Facade.tests
{
    public class FunctionTests
    {
        private readonly ITestOutputHelper output;

        public FunctionTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task InvokeFunction()
        {
            var store = new FileModelStore<FacadeInputs>("./",true);

            // Create an input object with default values.
            var input = new FacadeInputs();

            // Invoke the function.
            // The function invocation uses a FileModelStore
            // which will write the resulting model to disk.
            // You'll find the model at "./model.gltf"
            var l = new InvocationWrapper<FacadeInputs,FacadeOutputs>(store, Facade.Execute);
            var output = await l.InvokeAsync(input);
            
            var json = output.model.ToJson();
            File.WriteAllText("../../../facade.json", json);
        }

        [Fact]
        public void ProfileFunction()
        {
        //     var sw = new Stopwatch();
        //     var store = new FileModelStore<FacadeInputs>("./",true);
        //     var sb = new StringBuilder();
        //     sb.AppendLine($"Iteration,Execution(ms),glTF(ms),JSON(ms),JSON UpdateElements(ms),JSON AddElements(ms),IFC(ms),memory(scaled)");
        //     var maxTiming = double.NegativeInfinity;
        //     var maxMemory = double.NegativeInfinity;

        //     for(var i=1; i<=20; i++)
        //     {
        //         var input = new FacadeInputs(3.0, 0.1, 0.1, i, null, null, null, null, null, null);
        //         sw.Start();
        //         var output = Facade.Execute(new Dictionary<string, Elements.Model>(), input);
        //         sw.Stop();
        //         var execute = sw.ElapsedMilliseconds;
        //         maxTiming = Math.Max(maxTiming, execute);
        //         sw.Reset();

        //         sw.Start();
        //         output.model.ToGlTF("model.gltf");
        //         sw.Stop();
        //         var gltf = sw.ElapsedMilliseconds;
        //         maxTiming = Math.Max(maxTiming, gltf);
        //         sw.Reset();

        //         sw.Start();
        //         long timeToUpdateElements = 0;
        //         long timeToAddElements = 0;
        //         output.model.ToJson(out timeToUpdateElements, out timeToAddElements);
        //         sw.Stop();
        //         var json = sw.ElapsedMilliseconds;
        //         maxTiming = Math.Max(maxTiming, json);
        //         sw.Reset();

        //         sw.Start();
        //         output.model.ToIFC("model.ifc");
        //         sw.Stop();
        //         var ifc = sw.ElapsedMilliseconds;
        //         maxTiming = Math.Max(maxTiming, ifc);
        //         sw.Reset();

        //         var memory = System.GC.GetTotalMemory(true);
        //         maxMemory = Math.Max(maxMemory, memory);

        //         sb.AppendLine($"{i},{execute},{gltf},{json},{timeToUpdateElements},{timeToAddElements},{ifc},{(memory/maxMemory) * maxTiming}");
        //     }

        //     File.WriteAllText("profile.csv", sb.ToString());
        }
    }
}
