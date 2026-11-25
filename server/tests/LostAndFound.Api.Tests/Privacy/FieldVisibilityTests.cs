using System.Reflection;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;

namespace LostAndFound.Api.Tests.Privacy;

public sealed class FieldVisibilityTests
{
    [Fact]
    public void EveryEntityPropertyIsClassified()
    {
        var entityAssembly = typeof(User).Assembly;
        var entityTypes = entityAssembly
            .GetTypes()
            .Where(type => type.Namespace == typeof(User).Namespace
                && type.IsClass
                && !type.IsAbstract)
            .ToList();

        entityTypes.Should().NotBeEmpty();

        foreach (var entityType in entityTypes)
        {
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            properties.Should().NotBeEmpty($"{entityType.Name} should expose mapped properties.");

            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<FieldVisibilityAttribute>();
                attribute.Should().NotBeNull(
                    $"Entity property {entityType.Name}.{property.Name} must be decorated with [FieldVisibility].");
            }
        }
    }
}
