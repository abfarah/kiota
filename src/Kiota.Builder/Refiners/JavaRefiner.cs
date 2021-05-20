﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class JavaRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public override void Refine(CodeNamespace generatedCode)
        {
            AddInnerClasses(generatedCode);
            AndInsertOverrideMethodForRequestExecutorsAndBuilders(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode);
            ConvertUnionTypesToWrapper(generatedCode);
            AddRequireNonNullImports(generatedCode);
            FixReferencesToEntityType(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            AddDefaultImports(generatedCode, defaultNamespaces, defaultNamespacesForModels, defaultNamespacesForRequestBuilders);
            CorrectCoreType(generatedCode);
            PatchHeaderParametersType(generatedCode);
            AddListImport(generatedCode);
            AddParsableInheritanceForModelClasses(generatedCode);
            ConvertDeserializerPropsToMethods(generatedCode, "get");
            ReplaceBinaryByNativeType(generatedCode, "InputStream", "java.io", true);
            AddEnumSetImport(generatedCode);
        }
        private static void AddEnumSetImport(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.ClassKind == CodeClassKind.Model &&
                currentClass.GetChildElements(true).OfType<CodeProperty>().Any(x => x.Type is CodeType xType && xType.TypeDefinition is CodeEnum xEnumType && xEnumType.Flags)) {
                    var nUsing = new CodeUsing(currentClass) {
                        Name = "EnumSet",
                    };
                    nUsing.Declaration = new CodeType(nUsing) { Name = "java.util", IsExternal = true };
                    currentClass.AddUsing(nUsing);
                }

            CrawlTree(currentElement, AddEnumSetImport);
        }
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.ClassKind == CodeClassKind.Model) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.Implements.Add(new CodeType(currentClass) {
                    IsExternal = true,
                    Name = $"Parsable",
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        private static void AddListImport(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass) {
                var childElements = currentClass.GetChildElements(true);
                if(childElements.OfType<CodeProperty>().Any(x => x.Type.CollectionKind == CodeType.CodeTypeCollectionKind.Complex) ||
                    childElements.OfType<CodeMethod>().Any(x => x.ReturnType.CollectionKind == CodeType.CodeTypeCollectionKind.Complex) ||
                    childElements.OfType<CodeMethod>().Any(x => x.Parameters.Any(y => y.Type.CollectionKind == CodeType.CodeTypeCollectionKind.Complex))) {
                        var nUsing = new CodeUsing(currentClass) {
                            Name = "List"
                        };
                        nUsing.Declaration = new CodeType(nUsing) { Name = "java.util", IsExternal = true };
                        currentClass.AddUsing(nUsing);
                }
            }
            CrawlTree(currentElement, AddListImport);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "com.microsoft.kiota"),
            new ("HttpMethod", "com.microsoft.kiota"),
            new ("RequestInfo", "com.microsoft.kiota"),
            new ("ResponseHandler", "com.microsoft.kiota"),
            new ("QueryParametersBase", "com.microsoft.kiota"),
            new ("SerializationWriterFactory", "com.microsoft.kiota.serialization"),
            new ("Map", "java.util"),
            new ("URI", "java.net"),
            new ("URISyntaxException", "java.net"),
            new ("InputStream", "java.io"),
            new ("Function", "java.util.function"),
        };
        private static readonly Tuple<string, string>[] defaultNamespaces = new Tuple<string, string>[] { 
            new ("SerializationWriter", "com.microsoft.kiota.serialization"),
        };
        private static readonly Tuple<string, string>[] defaultNamespacesForModels = new Tuple<string, string>[] { 
            new ("ParseNode", "com.microsoft.kiota.serialization"),
            new ("Parsable", "com.microsoft.kiota.serialization"),
            new ("BiConsumer", "java.util.function"),
            new ("Map", "java.util"),
            new ("HashMap", "java.util"),
        };
        private static void CorrectCoreType(CodeElement currentElement) {
            if (currentElement is CodeProperty currentProperty) {
                if("IHttpCore".Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase))
                    currentProperty.Type.Name = "HttpCore";
                else if(currentProperty.Name.Equals("serializerFactory", StringComparison.OrdinalIgnoreCase))
                    currentProperty.Type.Name = "SerializationWriterFactory";
                else if(currentProperty.Name.Equals("deserializeFields", StringComparison.OrdinalIgnoreCase))
                    currentProperty.Type.Name = $"Map<String, BiConsumer<T, ParseNode>>";
                else if("DateTimeOffset".Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase)) {
                    currentProperty.Type.Name = $"OffsetDateTime";
                    var nUsing = new CodeUsing(currentProperty.Parent) {
                        Name = "OffsetDateTime",
                    };
                    nUsing.Declaration = new CodeType(nUsing) {
                        Name = "java.time",
                        IsExternal = true,
                    };
                    (currentProperty.Parent as CodeClass).AddUsing(nUsing);
                } else if (currentProperty.PropertyKind == CodePropertyKind.AdditionalData) {
                    currentProperty.Access = AccessModifier.Private;
                    currentProperty.DefaultValue = "new HashMap<>()";
                    currentProperty.Type.Name = "Map<String, Object>";
                    var parentClass = currentElement.Parent as CodeClass;
                    parentClass.AddMethod(new CodeMethod(parentClass) {
                        Name = $"get{currentProperty.Name.ToFirstCharacterUpperCase()}",
                        Access = AccessModifier.Public,
                        Description = currentProperty.Description,
                        MethodKind = CodeMethodKind.AdditionalDataAccessor,
                        IsAsync = false,
                        IsStatic = false,
                        ReturnType = currentProperty.Type,
                    });
                }
            }
            if (currentElement is CodeMethod currentMethod) {
                if(currentMethod.MethodKind == CodeMethodKind.RequestExecutor)
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler")).ToList().ForEach(x => x.Type.Name = "ResponseHandler");
                else if(currentMethod.MethodKind == CodeMethodKind.Serializer)
                    currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter")).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
            }
            CrawlTree(currentElement, CorrectCoreType);
        }
        private static void AddRequireNonNullImports(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.Parameters.Any(x => !x.Optional)) {
                var parentClass = currentMethod.Parent as CodeClass;
                var newUsing = new CodeUsing(parentClass) {
                    Name = "Objects",
                };
                newUsing.Declaration = new CodeType(newUsing) {
                    Name = "java.util",
                    IsExternal = true,
                };
                parentClass?.AddUsing(newUsing);
            }
            CrawlTree(currentElement, AddRequireNonNullImports);
        }
        private static void AndInsertOverrideMethodForRequestExecutorsAndBuilders(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass) {
                var codeMethods = currentClass.GetChildElements(true).OfType<CodeMethod>();
                if(codeMethods.Any()) {
                    var originalExecutorMethods = codeMethods.Where(x => x.MethodKind == CodeMethodKind.RequestExecutor);
                    var executorMethodsToAdd = originalExecutorMethods
                                        .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter))
                                        .Union(originalExecutorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers)))
                                        .Union(originalExecutorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers, CodeParameterKind.ResponseHandler)))
                                        .Where(x => x != null);
                    var originalGeneratorMethods = codeMethods.Where(x => x.MethodKind == CodeMethodKind.RequestGenerator);
                    var generatorMethodsToAdd = originalGeneratorMethods
                                        .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter))
                                        .Union(originalGeneratorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers)))
                                        .Where(x => x != null);
                    if(executorMethodsToAdd.Any() || generatorMethodsToAdd.Any())
                        currentClass.AddMethod(executorMethodsToAdd.Union(generatorMethodsToAdd).ToArray());
                }
            }
            
            CrawlTree(currentElement, AndInsertOverrideMethodForRequestExecutorsAndBuilders);
        }
        private static void PatchHeaderParametersType(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.Parameters.Any(x => x.ParameterKind == CodeParameterKind.Headers))
                currentMethod.Parameters.Where(x => x.ParameterKind == CodeParameterKind.Headers)
                                        .ToList()
                                        .ForEach(x => x.Type.Name = "Map<String, String>");
            CrawlTree(currentElement, PatchHeaderParametersType);
        }
        private static CodeMethod GetMethodClone(CodeMethod currentMethod, params CodeParameterKind[] parameterTypesToExclude) {
            if(currentMethod.Parameters.Any(x => parameterTypesToExclude.Contains(x.ParameterKind))) {
                var cloneMethod = currentMethod.Clone() as CodeMethod;
                cloneMethod.Parameters.RemoveAll(x => parameterTypesToExclude.Contains(x.ParameterKind));
                return cloneMethod;
            }
            else return null;
        }
    }
}
