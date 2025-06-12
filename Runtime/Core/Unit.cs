using System;

namespace Ludo.Reactive
{
    /// <summary>
    /// Represents a type that has only one value. This type is often used to denote the successful completion of a void-returning method.
    /// </summary>
    [Serializable]
    public struct Unit : IEquatable<Unit>
    {
        /// <summary>
        /// Gets the single unit value.
        /// </summary>
        public static readonly Unit Default = new Unit();

        /// <summary>
        /// Determines whether the specified Unit values are equal.
        /// </summary>
        /// <param name="first">The first Unit value to compare.</param>
        /// <param name="second">The second Unit value to compare.</param>
        /// <returns>true if the first Unit value is equal to the second Unit value; otherwise, false.</returns>
        public static bool operator ==(Unit first, Unit second)
        {
            return true;
        }

        /// <summary>
        /// Determines whether the specified Unit values are not equal.
        /// </summary>
        /// <param name="first">The first Unit value to compare.</param>
        /// <param name="second">The second Unit value to compare.</param>
        /// <returns>true if the first Unit value is not equal to the second Unit value; otherwise, false.</returns>
        public static bool operator !=(Unit first, Unit second)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Unit.
        /// </summary>
        /// <param name="obj">The object to compare with the current Unit.</param>
        /// <returns>true if the specified object is a Unit and is equal to the current Unit; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is Unit;
        }

        /// <summary>
        /// Determines whether the specified Unit is equal to the current Unit.
        /// </summary>
        /// <param name="other">The Unit to compare with the current Unit.</param>
        /// <returns>true if the specified Unit is equal to the current Unit; otherwise, false.</returns>
        public bool Equals(Unit other)
        {
            return true;
        }

        /// <summary>
        /// Returns the hash code for the current Unit.
        /// </summary>
        /// <returns>A hash code for the current Unit.</returns>
        public override int GetHashCode()
        {
            return 0;
        }

        /// <summary>
        /// Returns a string representation of the current Unit.
        /// </summary>
        /// <returns>String representation of the current Unit.</returns>
        public override string ToString()
        {
            return "()";
        }
    }
}
