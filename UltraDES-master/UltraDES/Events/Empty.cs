﻿// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020
using System;

namespace UltraDES
{
    
    /// <summary>
    /// (Serializable)a empty.
    /// </summary>
    /// <remarks>Lucas Alves, 11/01/2016.</remarks>
    [Serializable]
    public sealed class Empty : AbstractEvent
    {
        
        /// <summary>
        /// Constructor that prevents a default instance of this class from being created.
        /// </summary>
        /// <remarks>Lucas Alves, 11/01/2016.</remarks>
        private Empty() => Controllability = Controllability.Controllable;

        /// <summary>
        /// The instance
        /// </summary>
        private static readonly Empty Instance = new Empty();

        
        /// <summary>
        /// Gets the empty event.
        /// </summary>
        /// <value>The empty event.</value>
        public static Empty EmptyEvent => Instance;


        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            var p = obj as Empty;
            return (object) p != null;

        }

        
        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public override int GetHashCode() => "empty".GetHashCode();


        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        /// <remarks>Lucas Alves, 11/01/2016.</remarks>
        

        public override string ToString() => "\u2205";
    }
}