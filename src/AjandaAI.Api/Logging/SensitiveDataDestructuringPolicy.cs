// Serilog ile {@Nesne} olarak loglanan nesnelerdeki kişisel veri alanlarını otomatik maskeler.
// Elle maskelemeyi unutan log çağrıları için güvenlik ağıdır; esas yöntem MaskingHelper'dır.
// Hassas alan içermeyen nesnelerde false döner ve varsayılan destructuring devreye girer.

using System.Collections.Concurrent;
using System.Reflection;
using AjandaAI.Application.Common.Logging;
using Serilog.Core;
using Serilog.Events;

namespace AjandaAI.Api.Logging;

public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly Dictionary<string, Func<string?, string>> Maskers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Email"] = MaskingHelper.MaskEmail,
            ["Phone"] = MaskingHelper.MaskPhone,
            ["PhoneNumber"] = MaskingHelper.MaskPhone,
            ["Address"] = MaskingHelper.MaskAddress,
            ["DisplayName"] = v => MaskingHelper.MaskGeneric(v, 1, 0)
        };

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]?> SensitiveTypes = new();

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        var properties = SensitiveTypes.GetOrAdd(value.GetType(), GetPropertiesIfSensitive);
        if (properties is null)
        {
            result = null!;
            return false;
        }

        var logProperties = new List<LogEventProperty>(properties.Length);
        foreach (var property in properties)
        {
            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            var logValue = Maskers.TryGetValue(property.Name, out var mask)
                ? new ScalarValue(mask(propertyValue?.ToString()))
                : propertyValueFactory.CreatePropertyValue(propertyValue, destructureObjects: true);
            logProperties.Add(new LogEventProperty(property.Name, logValue));
        }

        result = new StructureValue(logProperties, value.GetType().Name);
        return true;
    }

    // Tipte en az bir hassas alan yoksa null döner (sonuç tip başına önbelleklenir).
    private static PropertyInfo[]? GetPropertiesIfSensitive(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            return null;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();
        return properties.Any(p => Maskers.ContainsKey(p.Name)) ? properties : null;
    }
}
