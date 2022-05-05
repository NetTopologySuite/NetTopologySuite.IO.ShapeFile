namespace NetTopologySuite.IO.Handlers
{
    /// <summary>
    /// Specifies the polygon building algorithm to use.
    /// </summary>
    /// <seealso href="https://gis.stackexchange.com/a/147971/26684">Rings order explained.</seealso>
    public enum PolygonBuilder
    {
        /// <summary>
        /// The default polygon builder to use. Defaults to <see cref="Legacy"/>.
        /// </summary>
        Default,

        /// <summary>
        /// Depends on Shapefile's ring orientation semantics.
        /// <list type="table">
        /// <listheader><term>Ring type</term><description>Orientation</description></listheader>
        /// <item><term>Shell</term><description>Clockwise, Shapefile's left-hand-rule</description></item>
        /// <item><term>Hole</term><description>Counter-Clockwise, Shapefile's right-hand-rule</description></item>
        /// </list>
        /// </summary>
        Legacy = Default,

        /// <summary>
        /// Here's the logic applied when the flag is enabled:
        /// <list type="number">
        /// <item>Considering all rings as a potential shell, search the valid holes for any possible shell.</item>
        /// <item>Check if the ring is inside any shell: if <c>true</c>, it can be considered a potential hole for the shell.</item>
        /// <item>Check if the ring is inside any hole of the shell: if <c>true</c>, this means that is actually a shell of a distinct geometry,
        /// and NOT a valid hole for the shell; a hole inside another hole is not allowed.</item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// Note that this experimental polygon builder is considerably slower
        /// - three to four times slower, in fact - than the <see cref="Legacy"/> polygon builder,
        /// especially for complex polygons (i.e.: polygons with a large number of holes).
        /// </remarks>
        Extended,

        /// <summary>
        /// No sorting of rings but assume that polygons are serialized
        /// in the following order: <c>Shell[, Holes][, Shell[, Holes][, ...]]</c>.
        /// This leads to the conclusion that the first ring that is not
        /// contained by the current polygon, is the start of a new polygon.
        /// </summary>
        Sequential,

        /// <summary>
        /// Uses <see cref="Operation.Polygonize.Polygonizer"/>
        /// </summary>
        UsePolygonizer


    }
}
