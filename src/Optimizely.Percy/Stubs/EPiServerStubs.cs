// EPiServer compatibility stubs — provides the minimal EPiServer CMS types needed
// for this package to compile without the full EPiServer.CMS.Core dependency.
// When deployed in a real Optimizely CMS 12 project, the actual EPiServer packages
// supply these types and these stubs are not shipped (they are internal).

using System;

#pragma warning disable CS1591

namespace EPiServer.Core
{
    /// <summary>Represents a content item in Optimizely CMS.</summary>
    public interface IContent
    {
        ContentReference ContentLink { get; set; }
        string Name { get; set; }
        ContentReference ParentLink { get; set; }
    }

    /// <summary>Reference to a content item.</summary>
    public class ContentReference : IEquatable<ContentReference>
    {
        public static readonly ContentReference EmptyReference = new ContentReference();

        public int ID { get; set; }
        public int WorkID { get; set; }
        public string? ProviderName { get; set; }

        public ContentReference() { }
        public ContentReference(int id) { ID = id; }
        public ContentReference(int id, int workId) { ID = id; WorkID = workId; }

        public bool IsNullOrEmpty() => ID == 0;

        public static bool IsNullOrEmpty(ContentReference? contentReference) =>
            contentReference == null || contentReference.ID == 0;

        public override string ToString() => ID.ToString();

        public bool Equals(ContentReference? other) =>
            other != null && ID == other.ID && WorkID == other.WorkID && ProviderName == other.ProviderName;

        public override bool Equals(object? obj) => obj is ContentReference cr && Equals(cr);

        public override int GetHashCode() => HashCode.Combine(ID, WorkID, ProviderName);

        public static bool operator ==(ContentReference? a, ContentReference? b) =>
            a is null ? b is null : a.Equals(b);

        public static bool operator !=(ContentReference? a, ContentReference? b) => !(a == b);
    }

    /// <summary>Event arguments for content events.</summary>
    public class ContentEventArgs : EventArgs
    {
        public IContent? Content { get; set; }
        public ContentReference? ContentLink { get; set; }
        public ContentReference? TargetLink { get; set; }
        public string? TransitionName { get; set; }
        public bool CancelAction { get; set; }
        public string? CancelReason { get; set; }
    }

    /// <summary>Event arguments for save events.</summary>
    public class SaveEventArgs : ContentEventArgs
    {
        public string? Action { get; set; }
    }

    /// <summary>Exposes content lifecycle events.</summary>
    public interface IContentEvents
    {
        event EventHandler<ContentEventArgs> PublishedContent;
        event EventHandler<ContentEventArgs> CreatedContent;
        event EventHandler<ContentEventArgs> DeletedContent;
        event EventHandler<SaveEventArgs> SavedContent;
        event EventHandler<ContentEventArgs> MovedContent;
    }

    /// <summary>Base interface for content data.</summary>
    public interface IContentData { }
}

namespace EPiServer.Web.Routing
{
    /// <summary>Resolves URLs for content items.</summary>
    public interface IUrlResolver
    {
        string? GetUrl(EPiServer.Core.IContent content);
        string? GetUrl(EPiServer.Core.ContentReference contentReference);
    }
}

namespace EPiServer.Framework
{
    /// <summary>Marks a class as an initializable module.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InitializableModuleAttribute : Attribute { }

    /// <summary>Declares a dependency on another module.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModuleDependencyAttribute : Attribute
    {
        public ModuleDependencyAttribute(Type dependencyType) { DependencyType = dependencyType; }
        public Type DependencyType { get; }
    }

    /// <summary>Represents a module that participates in CMS initialization.</summary>
    public interface IInitializableModule
    {
        void Initialize(InitializationEngine context);
        void Uninitialize(InitializationEngine context);
    }

    /// <summary>Context passed to initializable modules during startup/shutdown.</summary>
    public class InitializationEngine
    {
        public IServiceProvider? ServiceLocator { get; set; }
    }
}

namespace EPiServer.Web
{
    /// <summary>Stub for the EPiServer web initialization module (used as ModuleDependency target).</summary>
    public class InitializationModule { }
}

#pragma warning restore CS1591
