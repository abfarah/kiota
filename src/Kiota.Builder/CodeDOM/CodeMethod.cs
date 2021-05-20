﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    public enum CodeMethodKind
    {
        Custom,
        IndexerBackwardCompatibility,
        RequestExecutor,
        RequestGenerator,
        Serializer,
        DeserializerBackwardCompatibility,
        AdditionalDataAccessor
    }
    public enum HttpMethod {
        Get,
        Post,
        Patch,
        Put,
        Delete,
        Options,
        Connect,
        Head,
        Trace
    }

    public class CodeMethod : CodeTerminal, ICloneable, IDocumentedElement
    {
        public CodeMethod(CodeElement parent): base(parent)
        {
            
        }
        public HttpMethod? HttpMethod {get;set;}
        public CodeMethodKind MethodKind {get;set;} = CodeMethodKind.Custom;
        public AccessModifier Access {get;set;} = AccessModifier.Public;
        public CodeTypeBase ReturnType {get;set;}
        public List<CodeParameter> Parameters {get;set;} = new List<CodeParameter>();
        public bool IsStatic {get;set;} = false;
        public bool IsAsync {get;set;} = true;
        public string Description {get; set;}


        public object Clone()
        {
            return new CodeMethod(Parent) {
                MethodKind = MethodKind,
                ReturnType = ReturnType.Clone() as CodeTypeBase,
                Parameters = Parameters.Select(x => x.Clone() as CodeParameter).ToList(),
                Name = Name.Clone() as string,
                HttpMethod = HttpMethod,
                IsAsync = IsAsync,
                Access = Access,
                IsStatic = IsStatic,
                Description = Description?.Clone() as string,
                GenerationProperties = new (GenerationProperties),
            };
        }

        internal void AddParameter(params CodeParameter[] methodParameters)
        {
            if(!methodParameters.Any() || methodParameters.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(methodParameters));
            AddMissingParent(methodParameters);
            Parameters.AddRange(methodParameters);
        }
    }
}
