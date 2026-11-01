namespace AuthServer.Web;
using TypeGen.Core.Extensions;
using TypeGen.Core.SpecGeneration;


public class AdminUiGenerationSpec : GenerationSpec
{
    public override void OnBeforeGeneration(OnBeforeGenerationArgs args)
    {
        var types = GetType().Assembly.GetLoadableTypes()
            .Where(x => x.FullName != null
                 && (x.FullName.StartsWith("AuthServer.Web.Dto.Admin") || x.FullName.StartsWith("AuthServer.Web.Dto.Shared"))
                 && !x.Name.EndsWith("Extensions")
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
