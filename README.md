Sticky ScrollView for Xamarin Android
=====================================
Allows you to mark ScrollView items as sticky, so they stick at the top of the ScrollView while scrolling until another sticky item pushes it away.
I ported the Java version from Emil Sjölander (https://github.com/emilsjolander/StickyScrollViewItems/) to C#/Xamarin.

Usage
-----
Copy the StickyScrollView.cs file to your project and the Attrs.xml file to the values folder of your project.

Replace the `ScrollView` with a `StickyScrollview`.
From this:
```xml
<ScrollView xmlns:android="http://schemas.android.com/apk/res/android"
	android:layout_height="match_parent" android:layout_width="match_parent">
	<!-- scroll view child goes here -->
</ScrollView>
```
to this:
```xml
<namespacelowercase.StickyScrollView xmlns:android="http://schemas.android.com/apk/res/android"
	android:layout_height="match_parent" android:layout_width="match_parent">
	<!-- scroll view child goes here -->
</ScrollView>
```

The StickyScrollView inherits from ScrollView so you can only add one child but the child can have some more children. In this example a LinearLayout with a few views. One or more children can have an `android:tag` attribute with the value `sticky`, these will stick at the top of the ScrollView.
```xml
<namespacelowercase.StickyScrollView xmlns:android="http://schemas.android.com/apk/res/android"
	android:id="@+id/sticky_scroll"
	android:layout_height="match_parent" android:layout_width="match_parent">
	<LinearLayout 
		android:layout_height="match_parent" android:layout_width="match_parent" 
		android:orientation="horizontal">
		<!-- other children -->
		<View 
			android:layout_height="300dp" 
			android:layout_width="match_parent"
			android:tag="sticky"/>
		<!-- other children -->
	</LinearLayout>
</StickyScrollView>
```

There are also two additional flags that can be set on views that were added to optimize performance for the most usual cases. If the view you want to stick either has transparency or does not have a constant representation than you must supply one or both of the following flags. `-hastransparancy` for views that have transparancy and `-nonconstant` for views that will change appearance during there sticky time (examples are buttons with pressed states as well as progress spinners).

So this ends up with 4 different ways to tag a view as sticky resulting is slightly different behaviour `android:tag="sticky"` `android:tag="sticky-hastransparancy"` `android:tag="sticky-nonconstant"` and `android:tag="sticky-hastransparancy-nonconstant"`.
