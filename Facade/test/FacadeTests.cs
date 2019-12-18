using Xunit;
using Hypar.Functions.Execution.Local;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System.Collections.Generic;
using Hypar.Functions.Execution;
using System.IO;
using Elements;
using Elements.Serialization.glTF;

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
        public void GHLevelsByEnvelopeTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../TestOutput/GHmodel.json"));
            var inputs = new FacadeInputs(2.0, 0.1, 0.1, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = Facade.Execute(new Dictionary<string, Model> { { "Envelope", model }, { "Levels", model } }, inputs);
            System.IO.File.WriteAllText("../../../../../TestOutput/GHFacade.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../TestOutput/GHFacade.glb");
        }

    }
}
