﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ImageMagitek.Project
{
    public abstract class ProjectResourceBase: IEnumerable<ProjectResourceBase>
    {
        /// <summary>
        /// Identifying name of the resource
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public ProjectResourceBase Parent { get; set; }

        /// <summary>
        /// The child resources
        /// </summary>
        internal Dictionary<string, ProjectResourceBase> ChildResources = new Dictionary<string, ProjectResourceBase>();

        /// <summary>
        /// Determines if the ProjectResource can contain child resources
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can contain child resources; otherwise, <c>false</c>.
        /// </value>
        public bool CanContainChildResources { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the ProjectResource should be serialized.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [should be serialized]; otherwise, <c>false</c>.
        /// </value>
        public bool ShouldBeSerialized { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is leased.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is leased; otherwise, <c>false</c>.
        /// </value>
        public bool IsLeased { get; set; } = false;

        /// <summary>
        /// Rename a resource with a new name
        /// </summary>
        /// <param name="name">The new name.</param>
        public virtual void Rename(string name) => Name = name;

        /// <summary>
        /// Deep-clone copy of the object
        /// </summary>
        /// <returns></returns>
        public abstract ProjectResourceBase Clone();

        /// <summary>
        /// Serializes resource into an XElement
        /// </summary>
        /// <returns></returns>
        public abstract XElement Serialize();

        /// <summary>
        /// Deserialize XElement into ProjectResource
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public abstract bool Deserialize(XElement element);

        public IEnumerator<ProjectResourceBase> GetEnumerator() => ChildResources.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns the key of the ProjectResourceBase
        /// </summary>
        /// <returns></returns>
        public string ResourceKey
        {
            get
            {
                string key = Name;
                ProjectResourceBase parentVisitor = Parent;

                while (parentVisitor != null)
                {
                    key = Path.Combine(parentVisitor.Name, key);
                    parentVisitor = parentVisitor.Parent;
                }

                return key;
            }
        }

        /// <summary>
        /// Gets the resource relative to this resource by the relative key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="relativeKey">Key containing the relative path</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public T GetResourceRelative<T>(string relativeKey) where T : ProjectResourceBase
        {
            if (string.IsNullOrWhiteSpace(relativeKey))
                throw new ArgumentException();

            var nodeVisitor = this as ProjectResourceBase;
            string[] paths;

            if(relativeKey.StartsWith("\\")) // Relative to root
            {
                while (nodeVisitor.Parent != null)
                    nodeVisitor = nodeVisitor.Parent as ProjectResourceBase;
                paths = relativeKey.Substring(1).Split('\\');
            }
            else
                paths = relativeKey.Split('\\');

            foreach (var path in paths)
            {
                if (path == "..")
                    nodeVisitor = nodeVisitor.Parent as ProjectResourceBase;
                else if (nodeVisitor.ChildResources.ContainsKey(path))
                    nodeVisitor = nodeVisitor.ChildResources[path] as ProjectResourceBase;
                else
                    throw new KeyNotFoundException();
            }

            return nodeVisitor as T;
        }
    }
}
