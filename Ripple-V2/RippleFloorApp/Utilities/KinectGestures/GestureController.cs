﻿using Microsoft.Kinect;
using System;
using System.Collections.Generic;

namespace RippleFloorApp.Utilities.KinectGestures
{
    public class GestureController
    {
        /// <summary>
        /// The list of all gestures we are currently looking for
        /// </summary>
        private List<Gesture> gestures = new List<Gesture>();

        /// <summary>
        /// Initializes a new instance of the <see cref="GestureController"/> class.
        /// </summary>
        public GestureController()
        {
        }

        /// <summary>
        /// Occurs when [gesture recognised].
        /// </summary>
        public event EventHandler<GestureEventArgs> GestureRecognized;

        /// <summary>
        /// Updates all gestures.
        /// </summary>
        /// <param name="data">The skeleton data.</param>
        public void UpdateAllGestures(Body data)
        {
            foreach (var gesture in gestures)
            {
                gesture.UpdateGesture(data);
            }
        }

        /// <summary>
        /// Adds the gesture.
        /// </summary>
        /// <param name="name">The gesture type.</param>
        /// <param name="gestureDefinition">The gesture definition.</param>
        public void AddGesture(string name, IRelativeGestureSegment[] gestureDefinition)
        {
            var gesture = new Gesture(name, gestureDefinition);
            gesture.GestureRecognized += OnGestureRecognized;
            gestures.Add(gesture);
        }

        /// <summary>
        /// Handles the GestureRecognized event of the g control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="KinectSkeltonTracker.GestureEventArgs"/> instance containing the event data.</param>
        private void OnGestureRecognized(object sender, GestureEventArgs e)
        {
            if (GestureRecognized != null)
            {
                GestureRecognized(this, e);
            }

            foreach (var g in gestures)
            {
                g.Reset();
            }
        }
    }
}
