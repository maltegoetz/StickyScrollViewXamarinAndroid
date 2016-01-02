using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System.Collections.Generic;

/// <summary>
/// Java Code by Emil Sjolander - sjolander.emil@gmail.com https://github.com/emilsjolander/StickyScrollViewItems
/// licensed under Apache Version 2.0
/// ported to C#/Xamarin by Malte Götz https://github.com/maltegoetz/StickyScrollViewXamarinAndroid
/// </summary>
namespace StickyScrollViewXamarinDroid.StickyScrollView
{
    public class StickyScrollView : ScrollView
    {
        private const string StickyTag = "sticky";

        /**
	     * Flag for views that should stick and have non-constant drawing. e.g. Buttons, ProgressBars etc
	     */
        private const string FlagNonConstant = "-nonconstant";

        /**
	     * Flag for views that have aren't fully opaque
	     */
        private const string FlagHashTransparency = "-hastransparancy";

        /**
	     * Default height of the shadow peeking out below the stuck view.
	     */
        private const int DefaultShadowHeight = 10; // dp;

        private List<View> _stickyViews;
        private View _currentlyStickingView;
        private float _stickyViewTopOffset;
        private int _stickyViewLeftOffset;
        private bool _redirectTouchesToStickyView;
        private bool _clippingToPadding;
        private bool _clipToPaddingHasBeenSet;
        private bool _hasNotDoneActionDown = true;

        private int _mShadowHeight;
        private Drawable _mShadowDrawable;

        private readonly IRunnable _invalidateRunnable;

        private class RunnableAnonymousInnerClassHelper : Java.Lang.Object, Java.Lang.IRunnable
        {
            private readonly StickyScrollView _ssv;

            public RunnableAnonymousInnerClassHelper(StickyScrollView ssv)
            {
                _ssv = ssv;
            }

            public void Run()
            {
                if (_ssv._currentlyStickingView != null)
                {
                    int l = _ssv.GetLeftForViewRelativeOnlyChild(_ssv._currentlyStickingView);
                    int t = _ssv.GetBottomForViewRelativeOnlyChild(_ssv._currentlyStickingView);
                    int r = _ssv.GetRightForViewRelativeOnlyChild(_ssv._currentlyStickingView);
                    int b = (int)(_ssv.ScrollY + (_ssv._currentlyStickingView.Height + _ssv._stickyViewTopOffset));
                    _ssv.Invalidate(l, t, r, b);
                }
                _ssv.PostDelayed(this, 16);
            }
        }

        public StickyScrollView(Context context) : this(context, null)
        {
        }

        public StickyScrollView(Context context, IAttributeSet attrs) : this(context, attrs, Android.Resource.Attribute.ScrollViewStyle)
        {
        }

        public StickyScrollView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            _invalidateRunnable = new RunnableAnonymousInnerClassHelper(this);
            Setup();
            TypedArray a = context.ObtainStyledAttributes(attrs,
                    Resource.Styleable.StickyScrollView, defStyle, 0);

            float density = context.Resources.DisplayMetrics.Density;
            int defaultShadowHeightInPix = (int)(DefaultShadowHeight * density + 0.5f);

            _mShadowHeight = a.GetDimensionPixelSize(
                Resource.Styleable.StickyScrollView_stuckShadowHeight,
                defaultShadowHeightInPix);

            int shadowDrawableRes = a.GetResourceId(Resource.Styleable.StickyScrollView_stuckShadowDrawable, -1);

            if (shadowDrawableRes != -1)
            {
                _mShadowDrawable = context.Resources.GetDrawable(shadowDrawableRes);
            }

            a.Recycle();

        }

        /**
            * Sets the height of the shadow drawable in pixels.
            *
            * @param height
            */
        public void SetShadowHeight(int height)
        {
            _mShadowHeight = height;
        }


        public void Setup()
        {
            _stickyViews = new List<View>();
        }

        protected int GetLeftForViewRelativeOnlyChild(View v)
        {
            int left = v.Left;
            while (v.Parent != GetChildAt(0))
            {
                v = (View)v.Parent;
                left += v.Left;
            }
            return left;
        }

        private int GetTopForViewRelativeOnlyChild(View v)
        {
            int top = v.Top;
            while (v.Parent != GetChildAt(0))
            {
                v = (View)v.Parent;
                top += v.Top;
            }
            return top;
        }

        private int GetRightForViewRelativeOnlyChild(View v)
        {
            int right = v.Right;
            while (v.Parent != GetChildAt(0))
            {
                v = (View)v.Parent;
                right += v.Right;
            }
            return right;
        }

        private int GetBottomForViewRelativeOnlyChild(View v)
        {
            int bottom = v.Bottom;
            while (v.Parent != GetChildAt(0))
            {
                v = (View)v.Parent;
                bottom += v.Bottom;
            }
            return bottom;
        }
        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);
            if (!_clipToPaddingHasBeenSet)
            {
                _clippingToPadding = true;
            }
            NotifyHierarchyChanged();
        }
        public override void SetClipToPadding(bool clipToPadding)
        {
            base.SetClipToPadding(clipToPadding);
            _clippingToPadding = clipToPadding;
            _clipToPaddingHasBeenSet = true;
        }

        public override void AddView(View child)
        {
            base.AddView(child);
            FindStickyViews(child);
        }

        public override void AddView(View child, int index)
        {
            base.AddView(child, index);
            FindStickyViews(child);
        }

        public override void AddView(View child, int index, Android.Views.ViewGroup.LayoutParams parameters)
        {
            base.AddView(child, index, parameters);
            FindStickyViews(child);
        }

        public override void AddView(View child, int width, int height)
        {
            base.AddView(child, width, height);
            FindStickyViews(child);
        }

        public override void AddView(View child, Android.Views.ViewGroup.LayoutParams parameters)
        {
            base.AddView(child, parameters);
            FindStickyViews(child);
        }

        protected override void DispatchDraw(Canvas canvas)
        {
            base.DispatchDraw(canvas);
            if (_currentlyStickingView != null)
            {
                canvas.Save();
                canvas.Translate(PaddingLeft + _stickyViewLeftOffset, ScrollY + _stickyViewTopOffset + (_clippingToPadding ? PaddingTop : 0));

                canvas.ClipRect(0, (_clippingToPadding ? -_stickyViewTopOffset : 0),
                          Width - _stickyViewLeftOffset,
                          _currentlyStickingView.Height + _mShadowHeight + 1);

                if (_mShadowDrawable != null)
                {
                    int left = 0;
                    int right = _currentlyStickingView.Width;
                    int top = _currentlyStickingView.Height;
                    int bottom = _currentlyStickingView.Height + _mShadowHeight;
                    _mShadowDrawable.SetBounds(left, top, right, bottom);
                    _mShadowDrawable.Draw(canvas);
                }

                canvas.ClipRect(0, (_clippingToPadding ? -_stickyViewTopOffset : 0), Width, _currentlyStickingView.Height);
                if (GetStringTagForView(_currentlyStickingView).Contains(FlagHashTransparency))
                {
                    ShowView(_currentlyStickingView);
                    _currentlyStickingView.Draw(canvas);
                    HideView(_currentlyStickingView);
                }
                else {
                    _currentlyStickingView.Draw(canvas);
                }
                canvas.Restore();
            }
        }

        public override bool DispatchTouchEvent(MotionEvent ev)
        {
            if (ev.Action == MotionEventActions.Down)
            {
                _redirectTouchesToStickyView = true;
            }

            if (_redirectTouchesToStickyView)
            {
                _redirectTouchesToStickyView = _currentlyStickingView != null;
                if (_redirectTouchesToStickyView)
                {
                    _redirectTouchesToStickyView =
                        ev.GetY() <= (_currentlyStickingView.Height + _stickyViewTopOffset) &&
                        ev.GetX() >= GetLeftForViewRelativeOnlyChild(_currentlyStickingView) &&
                        ev.GetX() <= GetRightForViewRelativeOnlyChild(_currentlyStickingView);
                }
            }
            else if (_currentlyStickingView == null)
            {
                _redirectTouchesToStickyView = false;
            }
            if (_redirectTouchesToStickyView)
            {
                ev.OffsetLocation(0, -1 * ((ScrollY + _stickyViewTopOffset) - GetTopForViewRelativeOnlyChild(_currentlyStickingView)));
            }
            return base.DispatchTouchEvent(ev);
        }



        public bool ClipToPaddingHasBeenSet
        {
            get
            {
                return _clipToPaddingHasBeenSet;
            }

            set
            {
                _clipToPaddingHasBeenSet = value;
            }
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            if (_redirectTouchesToStickyView)
            {
                ev.OffsetLocation(0, ((ScrollY + _stickyViewTopOffset) - GetTopForViewRelativeOnlyChild(_currentlyStickingView)));
            }

            if (ev.Action == MotionEventActions.Down)
            {
                _hasNotDoneActionDown = false;
            }

            if (_hasNotDoneActionDown)
            {
                MotionEvent down = MotionEvent.Obtain(ev);
                down.Action = MotionEventActions.Down;
                base.OnTouchEvent(down);
                _hasNotDoneActionDown = false;
            }

            if (ev.Action == MotionEventActions.Up || ev.Action == MotionEventActions.Cancel)
            {
                _hasNotDoneActionDown = true;
            }

            return base.OnTouchEvent(ev);
        }

        protected override void OnScrollChanged(int l, int t, int oldl, int oldt)
        {
            base.OnScrollChanged(l, t, oldl, oldt);
            DoTheStickyThing();
        }

        private void DoTheStickyThing()
        {
            View viewThatShouldStick = null;
            View approachingView = null;
            foreach (View v in _stickyViews)
            {
                int viewTop = GetTopForViewRelativeOnlyChild(v) - ScrollY + (_clippingToPadding ? 0 : PaddingTop);
                if (viewTop <= 0)
                {
                    if (viewThatShouldStick == null || viewTop > (GetTopForViewRelativeOnlyChild(viewThatShouldStick) - ScrollY + (_clippingToPadding ? 0 : PaddingTop)))
                    {
                        viewThatShouldStick = v;
                    }
                }
                else {
                    if (approachingView == null || viewTop < (GetTopForViewRelativeOnlyChild(approachingView) - ScrollY + (_clippingToPadding ? 0 : PaddingTop)))
                    {
                        approachingView = v;
                    }
                }
            }
            if (viewThatShouldStick != null)
            {
                _stickyViewTopOffset = approachingView == null ? 0 : Java.Lang.Math.Min(0, GetTopForViewRelativeOnlyChild(approachingView) - ScrollY + (_clippingToPadding ? 0 : PaddingTop) - viewThatShouldStick.Height);
                if (viewThatShouldStick != _currentlyStickingView)
                {
                    if (_currentlyStickingView != null)
                    {
                        StopStickingCurrentlyStickingView();
                    }
                    // only compute the left offset when we start sticking.
                    _stickyViewLeftOffset = GetLeftForViewRelativeOnlyChild(viewThatShouldStick);
                    StartStickingView(viewThatShouldStick);
                }
            }
            else if (_currentlyStickingView != null)
            {
                StopStickingCurrentlyStickingView();
            }
        }

        private void StartStickingView(View viewThatShouldStick)
        {
            _currentlyStickingView = viewThatShouldStick;
            if (GetStringTagForView(_currentlyStickingView).Contains(FlagHashTransparency))
            {
                HideView(_currentlyStickingView);
            }
            if (((string)_currentlyStickingView.Tag).Contains(FlagNonConstant))
            {
                Post(_invalidateRunnable);
            }
        }

        private void StopStickingCurrentlyStickingView()
        {
            if (GetStringTagForView(_currentlyStickingView).Contains(FlagHashTransparency))
            {
                ShowView(_currentlyStickingView);
            }
            _currentlyStickingView = null;
            RemoveCallbacks(_invalidateRunnable);
        }

        /**
         * Notify that the sticky attribute has been added or removed from one or more views in the View hierarchy
         */
        public void NotifyStickyAttributeChanged()
        {
            NotifyHierarchyChanged();
        }

        private void NotifyHierarchyChanged()
        {
            if (_currentlyStickingView != null)
            {
                StopStickingCurrentlyStickingView();
            }
            _stickyViews.Clear();
            FindStickyViews(GetChildAt(0));
            DoTheStickyThing();
            Invalidate();
        }

        private void FindStickyViews(View v)
        {
            if (v is ViewGroup)
            {
                ViewGroup vg = (ViewGroup)v;
                for (int i = 0; i < vg.ChildCount; i++)
                {
                    string tag = GetStringTagForView(vg.GetChildAt(i));
                    if (tag != null && tag.Contains(StickyTag))
                    {
                        _stickyViews.Add(vg.GetChildAt(i));
                    }
                    else if (vg.GetChildAt(i) is ViewGroup)
                    {
                        FindStickyViews(vg.GetChildAt(i));
                    }
                }
            }
            else {
                string tag = (string)v.Tag;
                if (tag != null && tag.Contains(StickyTag))
                {
                    _stickyViews.Add(v);
                }
            }
        }

        private string GetStringTagForView(View v)
        {
            Java.Lang.Object tagObject = v.Tag;
            return Java.Lang.String.ValueOf(tagObject);
        }

        private void HideView(View v)
        {
            //TO-DO: handle SDK Version < 11
            //if (Build.VERSION.SdkInt >= 11)
            //{
            v.Alpha = 0;
            //}
            //else {
            //    AlphaAnimation anim = new AlphaAnimation(1, 0);
            //    anim.setDuration(0);
            //    anim.setFillAfter(true);
            //    v.startAnimation(anim);
            //}
        }

        private void ShowView(View v)
        {
            //TO-DO: handle SDK Version < 11
            //if (Build.VERSION.SDK_INT >= 11)
            //{
            v.Alpha = 1;
            //}
            //else {
            //    AlphaAnimation anim = new AlphaAnimation(0, 1);
            //    anim.setDuration(0);
            //    anim.setFillAfter(true);
            //    v.startAnimation(anim);
            //}
        }
    }
}