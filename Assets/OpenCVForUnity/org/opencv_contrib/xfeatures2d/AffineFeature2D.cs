
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.Xfeatures2dModule
{

    // C++: class AffineFeature2D
    /// <summary>
    ///  Class implementing affine adaptation for key points.
    /// </summary>
    /// <remarks>
    ///     A @ref FeatureDetector and a @ref DescriptorExtractor are wrapped to augment the
    ///     detected points with their affine invariant elliptic region and to compute
    ///     the feature descriptors on the regions after warping them into circles.
    ///    
    ///     The interface is equivalent to @ref Feature2D, adding operations for
    ///     @ref Elliptic_KeyPoint "Elliptic_KeyPoints" instead of @ref KeyPoint "KeyPoints".
    /// </remarks>
    public class AffineFeature2D : Feature2D
    {

        protected override void Dispose(bool disposing)
        {

            try
            {
                if (disposing)
                {
                }
                if (IsEnabledDispose)
                {
                    if (nativeObj != IntPtr.Zero)
                        xfeatures2d_AffineFeature2D_delete(nativeObj);
                    nativeObj = IntPtr.Zero;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }

        }

        protected internal AffineFeature2D(IntPtr addr) : base(addr) { }

        // internal usage only
        public static new AffineFeature2D __fromPtr__(IntPtr addr) { return new AffineFeature2D(addr); }

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        const string LIBNAME = "__Internal";
#else
        const string LIBNAME = "opencvforunity";
#endif



        // native support for java finalize()
        [DllImport(LIBNAME)]
        private static extern void xfeatures2d_AffineFeature2D_delete(IntPtr nativeObj);

    }
}
