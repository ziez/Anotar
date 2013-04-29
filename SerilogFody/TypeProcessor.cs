﻿using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaver
{
    void ProcessType(TypeDefinition type)
    {
        var fieldDefinition = type.Fields.FirstOrDefault(x => x.IsStatic && x.FieldType.FullName == loggerType.FullName);
        Action foundAction;
        if (fieldDefinition == null)
        {
            fieldDefinition = new FieldDefinition("AnotarLogger", FieldAttributes.Static | FieldAttributes.Private, loggerType)
                {
                    DeclaringType = type
                };
            foundAction = () => InjectField(type, fieldDefinition);
        }
        else
        {
            foundAction = () => { };
        }
        var fieldReference = fieldDefinition.GetGeneric();
        var foundUsage = false;
        foreach (var method in type.Methods)
        {
            //skip for abstract and delegates
            if (!method.HasBody)
            {
                continue;
            }

            var onExceptionProcessor = new OnExceptionProcessor
                {
                    Method = method,
                    Field = fieldReference,
                    FoundUsageInType = () => foundUsage = true,
                    ModuleWeaver = this
                };
            onExceptionProcessor.Process();

            var logForwardingProcessor = new LogForwardingProcessor
                {
					FoundUsageInType = () => foundUsage = true,
                    Method = method,
					ModuleWeaver = this,
                    Field = fieldReference,
                };
            logForwardingProcessor.ProcessMethod();

        }
        if (foundUsage)
        {
            foundAction();
        }
    }

    void InjectField(TypeDefinition type, FieldDefinition fieldDefinition)
	{
		var staticConstructor = type.GetStaticContructor();
	    var genericInstanceMethod = new GenericInstanceMethod(forContextDefinition);
	    genericInstanceMethod.GenericArguments.Add(type.GetGeneric());
	    var instructions = staticConstructor.Body.Instructions;
	    instructions.Insert(0, Instruction.Create(OpCodes.Call, genericInstanceMethod));
	    instructions.Insert(1, Instruction.Create(OpCodes.Stsfld, fieldDefinition.GetGeneric()));
		type.Fields.Add(fieldDefinition);
    }
}