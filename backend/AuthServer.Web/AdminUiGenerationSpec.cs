namespace AuthServer.Web;

using TypeGen.Core.Extensions;
using TypeGen.Core.SpecGeneration;


public class AdminUiGenerationSpec : GenerationSpec
{
    public override void OnBeforeGeneration(OnBeforeGenerationArgs args)
    {
        // Every type under an Application Messages namespace.
        var types = typeof(AuthServer.Application.DependencyInjection).Assembly.GetLoadableTypes()
            .Where(x => x.Namespace != null
                 && x.Namespace.StartsWith("AuthServer.Application.")
                 && x.Namespace.EndsWith(".Messages"))
            .Where(x => !x.Name.EndsWith("Extensions")
                 && !x.Name.EndsWith("Service")
                 && !x.IsNested
                 && !(x.IsAbstract && x.IsSealed)
                 && !x.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false));

        foreach (var type in types)
        {
            if (type.IsEnum)
                AddEnum(type, asUnionType: true);

            else if (type.IsClass)
                AddInterface(type);
        }
    }
}
