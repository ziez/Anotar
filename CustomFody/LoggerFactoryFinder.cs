using System.Linq;
using Mono.Cecil;

public partial class ModuleWeaver
{
    public MethodReference GetLoggerMethod;

    public void FindGetLoggerMethod()
    {
        var loggerFactoryAttribute = ModuleDefinition
            .Assembly
            .CustomAttributes
            .FirstOrDefault(x => x.AttributeType.Name == "LoggerFactoryAttribute");

        if (loggerFactoryAttribute == null)
        {
            LogInfo("Could not find a 'LoggerFactoryAttribute' on the current assembly. Going to search current assembly for 'LoggerFactory'.");

            var typeDefinition = ModuleDefinition
                .GetTypes()
                .FirstOrDefault(x => !x.IsGenericInstance && x.Name == "LoggerFactory");
            if (typeDefinition == null)
            {
                throw new WeavingException("Could not find a type named LoggerFactory");
            }

            FindGetLogger(typeDefinition);
        }
        else
        {
            var typeReference = (TypeReference) loggerFactoryAttribute.ConstructorArguments.First().Value;

            FindGetLogger(typeReference.Resolve());

            GetLoggerMethod = ModuleDefinition.Import(GetLoggerMethod);
            ModuleDefinition.Assembly.CustomAttributes.Remove(loggerFactoryAttribute);
        }

    }

    void FindGetLogger(TypeDefinition typeDefinition)
    {
        if (!typeDefinition.IsPublic)
        {
            var message = string.Format("The logger factory type '{0}' needs to be public.", typeDefinition.FullName);
            throw new WeavingException(message);
        }
        GetLoggerMethod = typeDefinition
            .Methods
            .FirstOrDefault(x =>
                x.Name ==  "GetLogger" && 
                x.IsStatic &&
                x.HasGenericParameters &&
                x.GenericParameters.Count == 1 &&
                x.Parameters.Count == 0);

        if (GetLoggerMethod == null)
        {
            throw new WeavingException("Found 'LoggerFactory' but it did not have a static 'GetLogger' method that takes 'string' as a parameter");
        }
        if (!GetLoggerMethod.Resolve().IsPublic)
        {
            var message = string.Format("The logger factory method '{0}' needs to be public.", GetLoggerMethod.FullName);
            throw new WeavingException(message);
        }
    }
}