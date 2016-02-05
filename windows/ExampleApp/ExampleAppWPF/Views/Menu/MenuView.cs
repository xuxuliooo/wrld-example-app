﻿using ExampleApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ExampleAppWPF
{
    public abstract class MenuView : Control
    {
        protected ControlClickHandler m_dragTabClickHandler = null;

        protected ListBox m_list = null;
        protected Image m_dragTabView;
        protected bool m_dragInProgress = false;
        protected bool m_loggingEnabled = false;

        protected IntPtr m_nativeCallerPointer;

        protected double m_stateChangeAnimationTimeMilliseconds = 200;
        protected double m_mainContainerVisibleOnScreenWhenClosedDip = 0;

        protected double m_mainContainerOffscreenOffsetXPx;

        protected double m_dragStartPosXPx;
        protected double m_controlStartPosXPx;

        protected double m_touchAnchorXPx;
        protected double m_touchAnchorYPx;

        protected double m_dragThresholdPx;

        protected bool m_dragAnchorSet = false;
        protected bool m_canBeginDrag = false;

        protected bool m_isFirstAnimationCeremony = true;

        protected List<CustomAppAnimation> m_menuAnimations = new List<CustomAppAnimation>();

        MainWindow m_mainWindow;

        protected abstract void RefreshListData(List<string> groups, List<bool> groupsExpandable, Dictionary<string, List<string>> groupToChildrenMap);

        static MenuView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MenuView), new FrameworkPropertyMetadata(typeof(MenuView)));
        }

        public MenuView(IntPtr nativeCallerPointer)
        {
            m_nativeCallerPointer = nativeCallerPointer;

            int dragThesholdDip = 10;
            m_dragThresholdPx = ConversionHelpers.AndroidToWindowsDip(dragThesholdDip);

            m_mainWindow = (MainWindow)Application.Current.MainWindow;
        }
        
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        protected void OnDragTabMouseClick(object sender, MouseButtonEventArgs e)
        {
            if(IsAnimating())
            {
                return;
            }

            MenuViewCLIMethods.ViewClicked(m_nativeCallerPointer);
        }

        protected void OnDragTabMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        protected void OnDragTabMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        public void Destroy()
        {
            //m_mainWindow.SizeChanged -= PerformLayout;
            m_mainWindow.MainGrid.Children.Remove(this);
        }

        public bool IsAnimating()
        {
            foreach (var anim in m_menuAnimations)
            {
                if (anim.m_animating)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsDragging()
        {
            return m_dragInProgress;
        }
        protected void MenuView_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsAnimating() || !CanInteract())
            {
                m_dragAnchorSet = false;
                return;
            }

            if (!m_canBeginDrag)
            {
                if(e.LeftButton == MouseButtonState.Released)
                {
                    // Click event, don't start drag
                    return;
                }

                m_canBeginDrag = MenuViewCLIMethods.TryBeginDrag(m_nativeCallerPointer);
            }

            var mousePosition = e.GetPosition(m_mainWindow.MainGrid);
            int xPx = (int)(mousePosition.X);
            int yPx = (int)(mousePosition.Y);

            // :TODO: need to re-write this across multiple events
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                m_touchAnchorXPx = xPx;
                m_touchAnchorYPx = yPx;
                m_dragAnchorSet = true;
            }
            else if (e.LeftButton == MouseButtonState.Released)
            {
                m_canBeginDrag = false;
                if (m_dragInProgress)
                {
                    HandleDragFinish(xPx, yPx);
                }
                else if (m_dragAnchorSet)
                {
                    RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                m_dragAnchorSet = false;
            }
            else
            {
                if (m_dragInProgress)
                {
                    HandleDragUpdate(xPx, yPx);
                }
                else if (m_canBeginDrag && m_dragAnchorSet)
                {
                    double mag = Math.Sqrt((m_touchAnchorXPx - xPx) * (m_touchAnchorXPx - xPx) + (m_touchAnchorYPx - yPx) * (m_touchAnchorYPx - yPx));

                    if (mag >= m_dragThresholdPx)
                    {
                        HandleDragStart(xPx, yPx);
                    }
                }
            }
        }

        protected void HandleDragStart(int xPx, int yPx)
        {
            m_dragInProgress = true;
            m_dragStartPosXPx = xPx;
            Debug.WriteLine("ACTION_DOWN x: {0}", xPx);

            MenuViewCLIMethods.ViewDragStarted(m_nativeCallerPointer);
        }

        protected abstract void HandleDragUpdate(int xPx, int yPx);
        protected abstract void HandleDragFinish(int xPx, int yPx);

        public void UpdateAnimation(float deltaSeconds)
        {
            var isClosed = false;
            var isOpen = false;

            foreach (var anim in m_menuAnimations)
            {
                Vector totalDelta = anim.m_animationEndPos - anim.m_animationStartPos;

                double totalDeltaLen = totalDelta.Length;
                bool done;

                if (totalDeltaLen > 0.001)
                {
                    double animationUnitsPerSecond = (totalDeltaLen / (m_stateChangeAnimationTimeMilliseconds / 1e3));
                    double frameDeltaUnits = animationUnitsPerSecond * deltaSeconds;
                    Vector norm = totalDelta / totalDeltaLen;
                    Vector delta = norm * frameDeltaUnits;
                    anim.m_animationCurrentPos += delta;

                    Vector currentPosDirToEnd = anim.m_animationEndPos - anim.m_animationCurrentPos;
                    currentPosDirToEnd.Normalize();

                    double dp = Vector.Multiply(currentPosDirToEnd, norm);
                    done = dp < 0.0;
                }
                else
                {
                    done = true;
                }

                AnimateToCurrentPos(false);

                if (done)
                {
                    anim.m_animationCurrentPos.X = anim.m_animationEndPos.X;
                    anim.m_animationCurrentPos.Y = anim.m_animationEndPos.Y;

                    AnimateToCurrentPos(false);

                    Debug.WriteLine("animation complete", "(" + anim.m_animationCurrentPos.X + "," + anim.m_animationCurrentPos.Y + ")");

                    bool closed = (anim.m_animationEndPos.X == anim.m_closedPos.X && anim.m_animationStartPos.X != anim.m_animationEndPos.X) ||
                                     (anim.m_animationEndPos.Y == anim.m_closedPos.Y && anim.m_animationStartPos.Y != anim.m_animationEndPos.Y);

                    bool open = (anim.m_animationEndPos.X == anim.m_openPos.X && anim.m_animationStartPos.X != anim.m_animationEndPos.X) ||
                                   (anim.m_animationEndPos.Y == anim.m_openPos.Y && anim.m_animationStartPos.Y != anim.m_animationEndPos.Y);

                    if(closed)
                    {
                        isClosed = true;
                    }
                    else if(open)
                    {
                        AnimateToCurrentPos(true);
                        isOpen = true;
                    }

                    anim.m_animating = false;
                } 
            }

            if (isClosed)
            {
                MenuViewCLIMethods.ViewCloseCompleted(m_nativeCallerPointer);
                m_mainWindow.EnableInput();
            }
            else if (isOpen)
            {
                MenuViewCLIMethods.ViewOpenCompleted(m_nativeCallerPointer);
                m_mainWindow.DisableInput();
            }
        }

        void AnimateToCurrentPos(bool animationCompleteAndOpen)
        {
            foreach (var anim in m_menuAnimations)
            {
                SetViewX(anim.m_animationCurrentPos.X);
                SetViewY(anim.m_animationCurrentPos.Y); 
            }

            m_list.IsHitTestVisible = animationCompleteAndOpen;
        }

        public float NormalisedAnimationProgress()
        {
            float totalDistance = (float)(m_menuAnimations[0].m_animationEndPos - m_menuAnimations[0].m_animationStartPos).Length;
            float currentDistance = (float)(m_menuAnimations[0].m_animationEndPos - m_menuAnimations[0].m_animationCurrentPos).Length;
            float result = currentDistance / totalDistance;
            result = Math.Max(Math.Min(result, 1.0f), 0.0f);

            if (m_menuAnimations[0].m_animationEndPos.X != m_menuAnimations[0].m_animationStartPos.X)
            {
                if (m_menuAnimations[0].m_animationEndPos.X == m_menuAnimations[0].m_openPos.X)
                {
                    result = 1.0f - result;
                }
            }
            else if (m_menuAnimations[0].m_animationEndPos.Y != m_menuAnimations[0].m_animationStartPos.Y)
            {
                if (m_menuAnimations[0].m_animationEndPos.Y == m_menuAnimations[0].m_openPos.Y)
                {
                    result = 1.0f - result;
                }
            }

            return result;
        }

        public virtual void AnimateToClosedOnScreen()
        {
            foreach (var anim in m_menuAnimations)
            {
                bool shouldRunAnimationBasedOnCurrentViewLocation = (!m_dragInProgress && ViewXPx(anim) != anim.m_closedPos.X);

                if (shouldRunAnimationBasedOnCurrentViewLocation || (anim.m_animating && anim.m_animationEndPos.X != anim.m_closedPos.X))
                {
                    double newXPx = anim.m_closedPos.X;
                    Debug.WriteLine("AnimateToClosedOnScreen x: {0}", newXPx);
                    AnimateViewToX(anim, newXPx);
                } 
            }
        }

        public virtual void AnimateToOpenOnScreen()
        {
            foreach (var anim in m_menuAnimations)
            {
                bool shouldRunAnimationBasedOnCurrentViewLocation = (!m_dragInProgress && ViewXPx(anim) != anim.m_openPos.X);

                if (shouldRunAnimationBasedOnCurrentViewLocation || (anim.m_animating && anim.m_animationEndPos.X != anim.m_openPos.X))
                {
                    double newXPx = anim.m_openPos.X;
                    Debug.WriteLine("AnimateToOpenOnScreen x: {0}", newXPx);
                    AnimateViewToX(anim, newXPx);
                }
            }
        }

        public void AnimateOffScreen()
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var anim in m_menuAnimations)
                {
                    bool shouldRunAnimationBasedOnCurrentViewLocation = (!m_dragInProgress && ViewXPx(anim) != anim.m_offscreenPos.X);

                    if (shouldRunAnimationBasedOnCurrentViewLocation || (anim.m_animating && anim.m_animationEndPos.X != anim.m_offscreenPos.X))
                    {
                        double newXPx = anim.m_offscreenPos.X;
                        Debug.WriteLine("AnimateOffScreen x: {0}", newXPx);
                        AnimateViewToX(anim, newXPx);
                    } 
                }
            });
        }

        public void AnimateToIntermediateOnScreenState(float onScreenState)
        {
            if (IsAnimating())
            {
                return;
            }

            foreach (var anim in m_menuAnimations)
            {
                double viewXPx = ViewXPx(anim);
                double newXPx = anim.m_offscreenPos.X + (((anim.m_closedPos.X - anim.m_offscreenPos.X) * onScreenState) + 0.5);

                if (!m_dragInProgress && viewXPx != newXPx)
                {
                    Debug.WriteLine("AnimateToIntermediateOnScreenState x: {0}", newXPx);
                    SetViewX(newXPx);
                } 
            }
        }

        public void AnimateToIntermediateOpenState(float openState)
        {
            if (IsAnimating())
            {
                return;
            }

            foreach (var anim in m_menuAnimations)
            {
                double newXPx = anim.m_closedPos.X + (((anim.m_openPos.X - anim.m_closedPos.X) * openState) + 0.5);

                if (!m_dragInProgress && ViewXPx(anim) != newXPx)
                {
                    Debug.WriteLine("AnimateToIntermediateOpenState x: {0}", newXPx);
                    SetViewX(newXPx);
                }
            }
        }

        public void PopulateData(
            IntPtr nativeCallerPointer,
            string[] groupNames,
            int[] groupSizes,
            bool[] groupIsExpandable,
            string[] childJson)
        {
            List<string> groups = groupNames.ToList();
            List<bool> groupsExpandable = groupIsExpandable.ToList();
            Dictionary<string, List<string>> childMap = new Dictionary<string, List<string>>();
            int childIndex = 0;
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                int size = groupSizes[groupIndex];
                List<string> children = new List<string>();
                for (int i = 0; i < size; i++)
                {
                    children.Add(childJson[childIndex]);
                    childIndex++;
                }

                childMap[groupNames[groupIndex]] = children;
            }

            RefreshListData(groups, groupsExpandable, childMap);
        }

        protected void AnimateViewToX(CustomAppAnimation anim, double xAsPx)
        {
            if (xAsPx == anim.m_offscreenPos.X || xAsPx == anim.m_closedPos.X)
            {
                anim.m_animationStartPos.X = m_isFirstAnimationCeremony ? anim.m_offscreenPos.X : anim.m_openPos.X;
            }
            else if (xAsPx == anim.m_openPos.X)
            {
                anim.m_animationStartPos.X = anim.m_closedPos.X;
            }
            else
            {
                throw new ArgumentException(string.Format("Invalid animation target {0} ", xAsPx));
            }

            m_isFirstAnimationCeremony = false;

            anim.m_animationStartPos.Y = anim.m_animationCurrentPos.Y = anim.m_animationEndPos.Y = ViewYPx(anim);

            anim.m_animationCurrentPos.X = ViewXPx(anim);
            anim.m_animationEndPos.X = xAsPx;

            anim.m_animating = true;
        }

        protected void AnimateViewToY(CustomAppAnimation anim, double yAsPx)
        {
            bool fromOffScreen = (ViewYPx(anim) == anim.m_offscreenPos.Y);

            if (yAsPx == anim.m_offscreenPos.Y || yAsPx == anim.m_closedPos.Y)
            {
                anim.m_animationStartPos.Y = (m_isFirstAnimationCeremony || fromOffScreen) ? anim.m_offscreenPos.Y : anim.m_openPos.Y;
            }
            else if (yAsPx == anim.m_openPos.Y)
            {
                anim.m_animationStartPos.Y = anim.m_closedPos.Y;
            }
            else
            {
                throw new ArgumentException(string.Format("Invalid animation target {0}", yAsPx));
            }

            m_isFirstAnimationCeremony = false;

            anim.m_animationStartPos.X =
                anim.m_animationCurrentPos.X =
                    anim.m_animationEndPos.X = ViewXPx(anim);

            anim.m_animationCurrentPos.Y = ViewYPx(anim);
            anim.m_animationEndPos.Y = yAsPx;

            anim.m_animating = true;
        }
        protected void SetViewX(double viewXPx)
        {
            var currentPosition = RenderTransform.Transform(new Point(0.0, 0.0));
            Debug.WriteLine("SetViewX {0}", viewXPx);
            RenderTransform = new TranslateTransform(viewXPx, currentPosition.Y);
        }

        protected void SetViewY(double viewYPx)
        {
            var currentPosition = RenderTransform.Transform(new Point(0.0, 0.0));
            Debug.WriteLine("SetViewY {0}", viewYPx);
            RenderTransform = new TranslateTransform(currentPosition.X, viewYPx);
        }

        protected int ViewXPx(CustomAppAnimation anim)
        {
            var currentPosition = anim.m_uiElement.RenderTransform.Transform(new Point(0.0, 0.0));
            int x = (int)currentPosition.X;
            return x;
        }

        protected int ViewYPx(CustomAppAnimation anim)
        {
            var currentPosition = anim.m_uiElement.RenderTransform.Transform(new Point(0.0, 0.0));
            int y = (int)currentPosition.Y;
            return y;
        }

        protected bool StartedClosed(double controlStartPosXDip)
        {
            double deltaClosed = Math.Abs(controlStartPosXDip - m_menuAnimations[0].m_closedPos.X);
            double deltaOpen = Math.Abs(controlStartPosXDip - m_menuAnimations[0].m_openPos.X);
            return deltaClosed < deltaOpen;
        }

        protected bool CanInteract()
        {
            return m_dragInProgress || IsClosed() || IsOpen();
        }

        protected bool IsClosed()
        {
            var currentPosition = RenderTransform.Transform(new Point(0.0, 0.0));
            return currentPosition.X == m_menuAnimations[0].m_closedPos.X;
        }

        protected bool IsOpen()
        {
            var currentPosition = RenderTransform.Transform(new Point(0.0, 0.0));
            return currentPosition.X == m_menuAnimations[0].m_openPos.X;
        }
    }
}
