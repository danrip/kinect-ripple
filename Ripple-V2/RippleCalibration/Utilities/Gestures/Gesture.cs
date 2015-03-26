using Microsoft.Kinect;
using System;

namespace RippleCalibration.Utilities.Gestures
{
    class Gesture
    {
        /// <summary>
        /// The parts that make up this gesture
        /// </summary>
        private IRelativeGestureSegment[] gestureParts;

        /// <summary>
        /// The current gesture part that we are matching against
        /// </summary>
        private int currentGesturePart = 0;

        /// <summary>
        /// the number of frames to pause for when a pause is initiated
        /// </summary>
        private int pausedFrameCount = 10;

        /// <summary>
        /// The current frame that we are on
        /// </summary>
        private int frameCount = 0;

        /// <summary>
        /// Are we paused?
        /// </summary>
        private bool paused = false;

        /// <summary>
        /// The name of gesture that this is
        /// </summary>
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Gesture"/> class.
        /// </summary>
        /// <param name="type">The type of gesture.</param>
        /// <param name="gestureParts">The gesture parts.</param>
        public Gesture(string name, IRelativeGestureSegment[] gestureParts)
        {
            this.gestureParts = gestureParts;
            this.name = name;
        }

        /// <summary>
        /// Occurs when [gesture recognised].
        /// </summary>
        public event EventHandler<GestureEventArgs> GestureRecognized;

        /// <summary>
        /// Updates the gesture.
        /// </summary>
        /// <param name="data">The skeleton data.</param>
        public void UpdateGesture(Body data)
        {
            if (paused)
            {
                if (frameCount == pausedFrameCount)
                {
                    paused = false;
                }

                frameCount++;
            }

            var result = gestureParts[currentGesturePart].CheckGesture(data);
            if (result == GesturePartResult.Succeed)
            {
                if (currentGesturePart + 1 < gestureParts.Length)
                {
                    currentGesturePart++;
                    frameCount = 0;
                    pausedFrameCount = 10;
                    paused = true;
                }
                else
                {
                    if (GestureRecognized != null)
                    {
                        GestureRecognized(this, new GestureEventArgs(name, data.TrackingId));
                        Reset();
                    }
                }
            }
            else if (result == GesturePartResult.Fail || frameCount == 50)
            {
                currentGesturePart = 0;
                frameCount = 0;
                pausedFrameCount = 5;
                paused = true;
            }
            else
            {
                frameCount++;
                pausedFrameCount = 5;
                paused = true;
            }
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            currentGesturePart = 0;
            frameCount = 0;
            pausedFrameCount = 5;
            paused = true;
        }
    }
}
