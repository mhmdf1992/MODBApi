using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MO.MODBApi.Attributes
{
    public class CommaSeparatedArrayModelBinder : IModelBinder
    {
        private static readonly Type[] supportedElementTypes = {
        typeof(int), typeof(long), typeof(short), typeof(byte),
        typeof(uint), typeof(ulong), typeof(ushort), typeof(Guid)
    };
        private static Array CopyAndConvertArray(IReadOnlyList<string> sourceArray, Type elementType)
        {
            var targetArray = Array.CreateInstance(elementType, sourceArray.Count);
            if (sourceArray.Count > 0)
            {
                var converter = TypeDescriptor.GetConverter(elementType);
                for (var i = 0; i < sourceArray.Count; i++)
                    targetArray.SetValue(converter.ConvertFromString(sourceArray[i]), i);
            }
            return targetArray;
        }

        internal static bool IsSupportedModelType(Type modelType)
        {
            return modelType.IsArray && modelType.GetArrayRank() == 1
                    && modelType.HasElementType
                    && supportedElementTypes.Contains(modelType.GetElementType());
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (!IsSupportedModelType(bindingContext.ModelType))
            {
                return Task.CompletedTask;
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            var stringArray = valueProviderResult.Values.FirstOrDefault()
                    ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (stringArray == null)
            {
                return Task.CompletedTask;
            }

            var elementType = bindingContext.ModelType.GetElementType();
            if (elementType == null)
            {
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(CopyAndConvertArray(stringArray, elementType));

            return Task.CompletedTask;
        }
    }

    public class CommaSeparatedArrayModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            return CommaSeparatedArrayModelBinder.IsSupportedModelType(context.Metadata.ModelType)
                    ? new CommaSeparatedArrayModelBinder() : null;
        }
    }
}
