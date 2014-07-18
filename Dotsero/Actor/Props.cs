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
    using System.Collections.Generic;

    /// <summary>
    /// Defines the Props used as properties for Actor initialization.
    /// </summary>
    public class Props
    {
        /// <summary>
        /// Gets a new Props with no Values.
        /// </summary>
        public static Props None { get { return new Props();  } }

        /// <summary>
        /// Gets the new Props with Values set to the
        /// passed arguments.
        /// </summary>
        /// <param name="arguments">the variable arguments to set as my Values</param>
        /// <returns>Props</returns>
        public static Props With(params object[] arguments)
        {
            return new Props(arguments);
        }

        /// <summary>
        /// Constructs a new Props with the variable arguments
        /// to set as my Values.
        /// </summary>
        /// <param name="arguments">the variable arguments to set as my Values</param>
        public Props(params object[] arguments)
        {
            Values = new List<object>(arguments.Length);

            foreach (object argument in arguments)
            {
                Values.Add(argument);
            }
        }

        /// <summary>
        /// Gets the Count of my Values.
        /// </summary>
        public int Count { get { return Values.Count; } }

        /// <summary>
        /// Gets and sets (privately) the IList of my Values.
        /// </summary>
        public IList<object> Values { get; private set; }
    }
}
