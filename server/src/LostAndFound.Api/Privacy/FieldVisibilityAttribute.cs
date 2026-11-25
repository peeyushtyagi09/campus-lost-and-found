namespace LostAndFound.Api.Privacy;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FieldVisibilityAttribute : Attribute
{
    public FieldVisibilityAttribute(Visibility visibility)
    {
        Visibility = visibility;
    }

    public Visibility Visibility { get; }
}
