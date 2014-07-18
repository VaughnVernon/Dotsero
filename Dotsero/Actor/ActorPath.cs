// Copyright 2012-2014 Vaughn Vernon
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Dotsero.Actor
{
    /// <summary>
    /// Defines an Actor's path.
    /// </summary>
    public class ActorPath
    {
        /// <summary>
        /// Gets my class' RootName.
        /// </summary>
        public static string RootName { get { return "/"; } }

        /// <summary>
        /// Gets my class' SystemName, which is the highest level name.
        /// </summary>
        public static string SystemName { get { return "@"; } }

        public string Name
        {
            get
            {
                int slash = Value.LastIndexOf("/");

                return Value.Substring(slash + 1);
            }
        }

        /// <summary>
        /// Gets and sets (privately) my Parent as a string.
        /// </summary>
        public string Parent { get; private set; }

        /// <summary>
        /// Gets and sets (privately) my Path as a string.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Answers whether or not this path is the root.
        /// </summary>
        /// <returns></returns>
        public bool IsRoot()
        {
            return Parent.Equals(SystemName) && Value.Equals(RootName);
        }

        public override string ToString()
        {
            return "ActorPath: " + Value;
        }

        /// <summary>
        /// Answers a new ActorPath composed from my Path
        /// as the parent path, and the name.
        /// </summary>
        /// <param name="name">the string name to append to the new path</param>
        /// <returns></returns>
        public ActorPath WithName(string name)
        {
            return new ActorPath(this.Value, name);
        }

        /// <summary>
        /// Constructs a new ActorPath from a parentPath and a name.
        /// </summary>
        /// <param name="parentPath">the string parent path</param>
        /// <param name="name">the string name</param>
        internal ActorPath(string parentPath, string name)
        {
            Parent = parentPath;

            if (IsRoot(parentPath))
            {
                Value = parentPath + name;
            }
            else if (IsRoot(name))
            {
                Value = name;
            }
            else
            {
                Value = parentPath + "/" + name;
            }
        }

        /// <summary>
        /// Constructs a new ActorPath from a parentPath.
        /// </summary>
        /// <param name="parentPath">the string parent path</param>
        internal ActorPath(string parentPath)
        {
            Parent = parentPath;

            Value = parentPath;
        }

        /// <summary>
        /// Answers whether or not a path is the root.
        /// </summary>
        /// <param name="path">the string path</param>
        /// <returns>bool</returns>
        private bool IsRoot(string path)
        {
            return path.Equals(RootName);
        }
    }
}
