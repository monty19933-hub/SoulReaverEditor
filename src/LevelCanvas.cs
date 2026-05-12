using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SoulReaverEditor
{
    internal enum LevelViewMode
    {
        TopDown,
        Orbit3D
    }

    internal sealed class LevelCanvas : Control
    {
        private readonly List<SR1LevelDocument> _documents = new List<SR1LevelDocument>();
        private readonly Dictionary<SR1LevelDocument, TerrainEdge[]> _terrainEdgeCache = new Dictionary<SR1LevelDocument, TerrainEdge[]>();
        private SR1LevelDocument _selectedDocument;
        private int _selectedObjectIndex = -1;
        private float _zoom = 1.0f;
        private PointF _pan = new PointF(0, 0);
        private ViewDragMode _viewDragMode = ViewDragMode.None;
        private bool _movingObject;
        private Point _lastMouse;
        private const float DefaultYaw = 0.75f;
        private const float DefaultPitch = 0.62f;
        private float _yaw = DefaultYaw;
        private float _pitch = DefaultPitch;

        public event EventHandler SelectedObjectChanged;
        public event EventHandler ObjectMoved;
        public event EventHandler<PortalActivatedEventArgs> PortalActivated;

        public bool ShowTerrain = true;
        public bool ShowObjects = true;
        public bool ShowPortals = true;
        public bool DragObjects;
        public LevelViewMode ViewMode = LevelViewMode.TopDown;

        public LevelCanvas()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(24, 26, 28);
            SetStyle(ControlStyles.ResizeRedraw, true);
            TabStop = true;
        }

        public SR1LevelDocument Document
        {
            get { return _documents.Count > 0 ? _documents[0] : null; }
            set
            {
                _documents.Clear();
                _terrainEdgeCache.Clear();
                if (value != null) _documents.Add(value);
                _selectedDocument = value;
                _selectedObjectIndex = -1;
                ResetView();
            }
        }

        public IList<SR1LevelDocument> Documents
        {
            get { return _documents.AsReadOnly(); }
        }

        public SR1LevelDocument SelectedDocument
        {
            get { return _selectedDocument; }
        }

        public int SelectedObjectIndex
        {
            get { return _selectedObjectIndex; }
            set
            {
                _selectedObjectIndex = value;
                if (_selectedDocument == null && _documents.Count > 0) _selectedDocument = _documents[0];
                Invalidate();
            }
        }

        public void SetScene(IList<SR1LevelDocument> documents, SR1LevelDocument selectedDocument)
        {
            _documents.Clear();
            _terrainEdgeCache.Clear();
            if (documents != null)
            {
                foreach (SR1LevelDocument doc in documents)
                {
                    if (doc != null) _documents.Add(doc);
                }
            }
            _selectedDocument = selectedDocument != null ? selectedDocument : (_documents.Count > 0 ? _documents[0] : null);
            _selectedObjectIndex = -1;
            ResetView();
        }

        public void SelectObject(SR1LevelDocument document, int objectIndex)
        {
            _selectedDocument = document;
            _selectedObjectIndex = objectIndex;
            Invalidate();
        }

        public void ResetView()
        {
            _zoom = 1.0f;
            _pan = new PointF(0, 0);
            _yaw = DefaultYaw;
            _pitch = DefaultPitch;
            Invalidate();
        }

        public void SetCameraAngles(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = pitch;
            ClampPitch();
            Invalidate();
        }

        public void RotateCamera(float yawDelta, float pitchDelta)
        {
            _yaw += yawDelta;
            _pitch += pitchDelta;
            ClampPitch();
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float oldZoom = _zoom;
            _zoom *= e.Delta > 0 ? 1.15f : 1.0f / 1.15f;
            if (_zoom < 0.1f) _zoom = 0.1f;
            if (_zoom > 30.0f) _zoom = 30.0f;

            if (ViewMode == LevelViewMode.TopDown && _documents.Count > 0)
            {
                ProjectionContext beforeContext = CreateProjectionContext(oldZoom);
                ProjectionContext afterContext = CreateProjectionContext(_zoom);
                PointF before = ScreenToWorldTopDown(beforeContext, e.Location);
                PointF after = ScreenToWorldTopDown(afterContext, e.Location);
                _pan.X += (after.X - before.X) * afterContext.Scale;
                _pan.Y += (after.Y - before.Y) * afterContext.Scale;
            }
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (e.Button == MouseButtons.Left)
            {
                HitResult hit = HitObject(e.Location);
                if (hit.Document != null)
                {
                    _selectedDocument = hit.Document;
                    _selectedObjectIndex = hit.ObjectIndex;
                    if (SelectedObjectChanged != null) SelectedObjectChanged(this, EventArgs.Empty);
                    if (DragObjects && ViewMode == LevelViewMode.TopDown)
                    {
                        _movingObject = true;
                        Capture = true;
                    }
                    Invalidate();
                    return;
                }

                if (ViewMode == LevelViewMode.Orbit3D)
                {
                    BeginViewDrag(e.Location, ViewDragMode.Orbit);
                    return;
                }
            }

            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
            {
                BeginViewDrag(e.Location, ViewDragMode.Pan);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_movingObject && _selectedDocument != null && _selectedObjectIndex >= 0 && _selectedObjectIndex < _selectedDocument.Objects.Count)
            {
                PointF world = ScreenToWorldTopDown(CreateProjectionContext(_zoom), e.Location);
                LevelObjectPlacement obj = _selectedDocument.Objects[_selectedObjectIndex];
                short nextX = ClampShort(world.X);
                short nextZ = ClampShort(world.Y);
                int deltaX = nextX - obj.X;
                int deltaZ = nextZ - obj.Z;
                bool moveSpectral = obj.HasSpectralPosition;
                obj.X = nextX;
                obj.Z = nextZ;
                if (moveSpectral)
                {
                    obj.SpectralX = AddShortDelta(obj.SpectralX, deltaX);
                    obj.SpectralZ = AddShortDelta(obj.SpectralZ, deltaZ);
                }
                SR1LevelParser.WriteObjectToRaw(_selectedDocument, obj);
                if (ObjectMoved != null) ObjectMoved(this, EventArgs.Empty);
                Invalidate();
                return;
            }

            if (_viewDragMode != ViewDragMode.None)
            {
                int dx = e.X - _lastMouse.X;
                int dy = e.Y - _lastMouse.Y;
                if (_viewDragMode == ViewDragMode.Orbit)
                {
                    RotateCamera(dx * 0.01f, dy * 0.01f);
                }
                else
                {
                    _pan.X += dx;
                    _pan.Y += dy;
                }
                _lastMouse = e.Location;
                Invalidate();
            }
        }

        private void BeginViewDrag(Point location, ViewDragMode mode)
        {
            _viewDragMode = mode;
            _lastMouse = location;
            Capture = true;
            Cursor = Cursors.SizeAll;
        }

        private void ClampPitch()
        {
            if (_pitch < -1.35f) _pitch = -1.35f;
            if (_pitch > 1.35f) _pitch = 1.35f;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _viewDragMode = ViewDragMode.None;
            _movingObject = false;
            Capture = false;
            Cursor = Cursors.Default;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left || !ShowPortals) return;

            PortalHitResult hit = HitPortal(e.Location);
            if (hit.Document == null || hit.Portal == null) return;

            _viewDragMode = ViewDragMode.None;
            _movingObject = false;
            Capture = false;
            Cursor = Cursors.Default;
            if (PortalActivated != null) PortalActivated(this, new PortalActivatedEventArgs(hit.Document, hit.Portal));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush bg = new SolidBrush(BackColor))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            DrawGrid(g);

            if (_documents.Count == 0)
            {
                DrawCenteredText(g, "Load a room from the Level Editor tab.");
                return;
            }

            ProjectionContext context = CreateProjectionContext(_zoom);
            if (ShowTerrain) DrawTerrain(g, context);
            if (ShowPortals) DrawPortals(g, context);
            if (ShowObjects) DrawObjects(g, context);
            DrawOrientationCues(g, context);
            DrawOverlay(g);
        }

        private void DrawTerrain(Graphics g, ProjectionContext context)
        {
            SmoothingMode oldSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            for (int d = 0; d < _documents.Count; d++)
            {
                SR1LevelDocument doc = _documents[d];
                PointF[] projectedVertices = ProjectVertices(context, doc);
                Color tint = RoomColor(d, 70);
                using (Pen edge = new Pen(Color.FromArgb(120, tint), 1))
                {
                    if (ViewMode == LevelViewMode.TopDown && context.TotalTriangles <= 25000)
                    {
                        using (GraphicsPath fillPath = new GraphicsPath())
                        using (SolidBrush fill = new SolidBrush(Color.FromArgb(18, tint)))
                        {
                            for (int i = 0; i < doc.Triangles.Count; i++)
                            {
                                LevelTriangle tri = doc.Triangles[i];
                                PointF a = projectedVertices[tri.A];
                                PointF b = projectedVertices[tri.B];
                                PointF c = projectedVertices[tri.C];
                                if (!TriangleNearViewport(a, b, c, context.Viewport)) continue;
                                fillPath.AddPolygon(new PointF[] { a, b, c });
                            }
                            if (fillPath.PointCount > 0) g.FillPath(fill, fillPath);
                        }
                    }

                    DrawTerrainEdges(g, edge, context, doc, projectedVertices);
                }
            }
            g.SmoothingMode = oldSmoothing;
        }

        private PointF[] ProjectVertices(ProjectionContext context, SR1LevelDocument doc)
        {
            PointF[] points = new PointF[doc.Vertices.Count];
            for (int i = 0; i < doc.Vertices.Count; i++)
            {
                LevelVertex vertex = doc.Vertices[i];
                points[i] = Project(context, vertex.X, vertex.Y, vertex.Z);
            }
            return points;
        }

        private void DrawTerrainEdges(Graphics g, Pen edge, ProjectionContext context, SR1LevelDocument doc, PointF[] projectedVertices)
        {
            TerrainEdge[] terrainEdges = GetTerrainEdges(doc);
            using (GraphicsPath path = new GraphicsPath())
            {
                int pending = 0;
                for (int i = 0; i < terrainEdges.Length; i++)
                {
                    TerrainEdge terrainEdge = terrainEdges[i];
                    if (terrainEdge.A < 0 || terrainEdge.A >= projectedVertices.Length) continue;
                    if (terrainEdge.B < 0 || terrainEdge.B >= projectedVertices.Length) continue;

                    PointF a = projectedVertices[terrainEdge.A];
                    PointF b = projectedVertices[terrainEdge.B];
                    if (!SegmentNearViewport(a, b, context.Viewport)) continue;

                    path.StartFigure();
                    path.AddLine(a, b);
                    pending++;

                    if (pending >= 35000)
                    {
                        g.DrawPath(edge, path);
                        path.Reset();
                        pending = 0;
                    }
                }

                if (pending > 0) g.DrawPath(edge, path);
            }
        }

        private TerrainEdge[] GetTerrainEdges(SR1LevelDocument doc)
        {
            TerrainEdge[] cached;
            if (_terrainEdgeCache.TryGetValue(doc, out cached)) return cached;

            HashSet<long> seen = new HashSet<long>();
            List<TerrainEdge> edges = new List<TerrainEdge>(doc.Triangles.Count * 2);
            for (int i = 0; i < doc.Triangles.Count; i++)
            {
                LevelTriangle tri = doc.Triangles[i];
                AddTerrainEdge(edges, seen, tri.A, tri.B);
                AddTerrainEdge(edges, seen, tri.B, tri.C);
                AddTerrainEdge(edges, seen, tri.C, tri.A);
            }

            cached = edges.ToArray();
            _terrainEdgeCache[doc] = cached;
            return cached;
        }

        private static void AddTerrainEdge(List<TerrainEdge> edges, HashSet<long> seen, int a, int b)
        {
            if (a == b) return;
            int low = Math.Min(a, b);
            int high = Math.Max(a, b);
            long key = ((long)low << 32) | (uint)high;
            if (seen.Add(key))
            {
                TerrainEdge edge = new TerrainEdge();
                edge.A = low;
                edge.B = high;
                edges.Add(edge);
            }
        }

        private void DrawPortals(Graphics g, ProjectionContext context)
        {
            using (Pen pen = new Pen(Color.FromArgb(220, 210, 120, 255), 2))
            using (Brush brush = new SolidBrush(Color.FromArgb(220, 230, 190, 255)))
            {
                foreach (SR1LevelDocument doc in _documents)
                {
                    foreach (LevelPortal portal in doc.Portals)
                    {
                        PointF p1 = Project(context, portal.MinX, portal.MinY, portal.MinZ);
                        PointF p2 = Project(context, portal.MaxX, portal.MaxY, portal.MaxZ);
                        RectangleF r = RectFromPoints(p1, p2);
                        if (r.Width < 6) r.Width = 6;
                        if (r.Height < 6) r.Height = 6;
                        if (!r.IntersectsWith(context.Viewport)) continue;
                        g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
                        g.DrawString(ZoneNamer.DisplayPortalTarget(portal.ToLevelName), Font, brush, r.Right + 4, r.Top);
                    }
                }
            }
        }

        private void DrawObjects(Graphics g, ProjectionContext context)
        {
            foreach (SR1LevelDocument doc in _documents)
            {
                for (int i = 0; i < doc.Objects.Count; i++)
                {
                    LevelObjectPlacement obj = doc.Objects[i];
                    bool selected = doc == _selectedDocument && i == _selectedObjectIndex;
                    PointF p = Project(context, obj.X, obj.Y, obj.Z);
                    PointF original = Project(context, obj.OriginalX, obj.OriginalY, obj.OriginalZ);
                    if (!PointNearViewport(p, context.Viewport, 24) && !selected) continue;

                    if (obj.HasChanged)
                    {
                        using (Pen movePen = new Pen(selected ? Color.FromArgb(235, 255, 220, 95) : Color.FromArgb(180, 180, 180, 180), selected ? 2.0f : 1.0f))
                        {
                            movePen.DashStyle = DashStyle.Dash;
                            g.DrawLine(movePen, original, p);
                        }

                        float oldRadius = selected ? 6 : 4;
                        using (Pen oldPen = new Pen(Color.FromArgb(selected ? 240 : 170, 255, 255, 255), selected ? 2.0f : 1.0f))
                        {
                            oldPen.DashStyle = DashStyle.Dot;
                            g.DrawEllipse(oldPen, original.X - oldRadius, original.Y - oldRadius, oldRadius * 2, oldRadius * 2);
                        }

                        if (selected)
                        {
                            using (Brush text = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                            {
                                g.DrawString("original", Font, text, original.X + 8, original.Y + 6);
                            }
                        }
                    }

                    float radius = selected ? 7 : 5;
                    Color color = selected ? Color.FromArgb(255, 255, 210, 90) : ObjectColor(obj.FileName);
                    using (Brush brush = new SolidBrush(color))
                    using (Pen pen = new Pen(selected ? Color.White : Color.FromArgb(230, 20, 20, 20), selected ? 2 : 1))
                    {
                        if (ViewMode == LevelViewMode.Orbit3D)
                        {
                            PointF floor = Project(context, obj.X, 0, obj.Z);
                            g.DrawLine(Pens.Gray, floor, p);
                        }
                        g.FillEllipse(brush, p.X - radius, p.Y - radius, radius * 2, radius * 2);
                        g.DrawEllipse(pen, p.X - radius, p.Y - radius, radius * 2, radius * 2);
                    }

                    if (selected || TotalObjectCount() < 120)
                    {
                        using (Brush text = new SolidBrush(Color.FromArgb(230, 245, 245, 245)))
                        {
                            g.DrawString(ObjectNamer.DisplayName(obj.FileName), Font, text, p.X + 8, p.Y - 8);
                        }
                    }
                }
            }
        }

        private void DrawOrientationCues(Graphics g, ProjectionContext context)
        {
            RectangleF bounds = context.Bounds;
            float centerX = bounds.Left + bounds.Width / 2.0f;
            float centerZ = bounds.Top + bounds.Height / 2.0f;
            float centerY = context.CenterY;

            PointF bottom = Project(context, centerX, centerY, bounds.Top);
            PointF top = Project(context, centerX, centerY, bounds.Bottom);
            PointF left = Project(context, bounds.Left, centerY, centerZ);
            PointF right = Project(context, bounds.Right, centerY, centerZ);

            Color zColor = Color.FromArgb(235, 100, 220, 255);
            Color xColor = Color.FromArgb(235, 255, 120, 120);
            Color yColor = Color.FromArgb(235, 125, 235, 150);

            using (Pen zPen = new Pen(zColor, 2.0f))
            using (Pen xPen = new Pen(xColor, 2.0f))
            {
                zPen.DashStyle = DashStyle.Dash;
                xPen.DashStyle = DashStyle.Dash;
                g.DrawLine(zPen, bottom, top);
                g.DrawLine(xPen, left, right);
            }

            DrawBadge(g, "TOP +Z", top, zColor, 0, -24, StringAlignment.Center);
            DrawBadge(g, "BOTTOM -Z", bottom, zColor, 0, 8, StringAlignment.Center);
            DrawBadge(g, "-X", left, xColor, -8, -10, StringAlignment.Far);
            DrawBadge(g, "+X", right, xColor, 8, -10, StringAlignment.Near);

            if (ViewMode == LevelViewMode.Orbit3D)
            {
                PointF low = Project(context, bounds.Right, context.MinY, bounds.Bottom);
                PointF high = Project(context, bounds.Right, context.MaxY, bounds.Bottom);
                using (Pen yPen = new Pen(yColor, 2.0f))
                {
                    yPen.DashStyle = DashStyle.Dash;
                    g.DrawLine(yPen, low, high);
                }
                DrawBadge(g, "UP +Y", high, yColor, 8, -18, StringAlignment.Near);
                DrawBadge(g, "DOWN -Y", low, yColor, 8, 6, StringAlignment.Near);
            }

            DrawViewCompass(g, xColor, yColor, zColor);
        }

        private void DrawViewCompass(Graphics g, Color xColor, Color yColor, Color zColor)
        {
            RectangleF panel = new RectangleF(Math.Max(12, Width - 154), 12, 142, 112);
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(190, 16, 18, 20)))
            using (Pen border = new Pen(Color.FromArgb(150, 255, 255, 255), 1))
            {
                g.FillRectangle(bg, panel);
                g.DrawRectangle(border, panel.X, panel.Y, panel.Width, panel.Height);
            }

            using (Brush text = new SolidBrush(Color.FromArgb(230, 245, 245, 245)))
            {
                g.DrawString("World axes", Font, text, panel.Left + 10, panel.Top + 8);
            }

            PointF origin = new PointF(panel.Left + 55, panel.Top + 70);
            DrawCompassAxis(g, origin, 44, 1, 0, 0, "+X", xColor);
            DrawCompassAxis(g, origin, 44, 0, 1, 0, "+Y", yColor);
            DrawCompassAxis(g, origin, 44, 0, 0, 1, "Top +Z", zColor);
        }

        private void DrawCompassAxis(Graphics g, PointF origin, float length, float dx, float dy, float dz, string label, Color color)
        {
            PointF projected = ProjectDirection(dx, dy, dz, length);
            PointF end = new PointF(origin.X + projected.X, origin.Y + projected.Y);
            using (AdjustableArrowCap cap = new AdjustableArrowCap(4, 5))
            using (Pen pen = new Pen(color, 2.0f))
            using (Brush brush = new SolidBrush(color))
            {
                pen.CustomEndCap = cap;
                g.DrawLine(pen, origin, end);
                g.DrawString(label, Font, brush, end.X + 4, end.Y - 8);
            }
        }

        private PointF ProjectDirection(float dx, float dy, float dz, float length)
        {
            if (ViewMode == LevelViewMode.TopDown)
            {
                return new PointF(dx * length, -dz * length);
            }

            float cosY = (float)Math.Cos(_yaw);
            float sinY = (float)Math.Sin(_yaw);
            float x1 = dx * cosY + dz * sinY;
            float z1 = -dx * sinY + dz * cosY;

            float cosP = (float)Math.Cos(_pitch);
            float sinP = (float)Math.Sin(_pitch);
            float y1 = dy * cosP - z1 * sinP;
            return new PointF(x1 * length, -y1 * length);
        }

        private void DrawBadge(Graphics g, string text, PointF point, Color color, float offsetX, float offsetY, StringAlignment alignment)
        {
            SizeF size = g.MeasureString(text, Font);
            float width = size.Width + 10;
            float height = size.Height + 5;
            float x = point.X + offsetX;
            if (alignment == StringAlignment.Center) x -= width / 2.0f;
            if (alignment == StringAlignment.Far) x -= width;
            float y = point.Y + offsetY;

            RectangleF rect = new RectangleF(x, y, width, height);
            rect = KeepInsideClient(rect);

            using (SolidBrush bg = new SolidBrush(Color.FromArgb(205, 16, 18, 20)))
            using (Pen border = new Pen(color, 1))
            using (Brush brush = new SolidBrush(color))
            {
                g.FillRectangle(bg, rect);
                g.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
                g.DrawString(text, Font, brush, rect.X + 5, rect.Y + 2);
            }
        }

        private RectangleF KeepInsideClient(RectangleF rect)
        {
            if (rect.Width > Width) rect.Width = Width;
            if (rect.Height > Height) rect.Height = Height;
            if (rect.Left < 2) rect.X = 2;
            if (rect.Top < 2) rect.Y = 2;
            if (rect.Right > Width - 2) rect.X = Width - rect.Width - 2;
            if (rect.Bottom > Height - 30) rect.Y = Height - rect.Height - 30;
            return rect;
        }

        private void DrawGrid(Graphics g)
        {
            using (Pen pen = new Pen(Color.FromArgb(35, 255, 255, 255), 1))
            {
                int step = 64;
                for (int x = -step; x > -Width; x -= step) g.DrawLine(pen, Width / 2 + x, 0, Width / 2 + x, Height);
                for (int x = 0; x < Width; x += step) g.DrawLine(pen, Width / 2 + x, 0, Width / 2 + x, Height);
                for (int y = -step; y > -Height; y -= step) g.DrawLine(pen, 0, Height / 2 + y, Width, Height / 2 + y);
                for (int y = 0; y < Height; y += step) g.DrawLine(pen, 0, Height / 2 + y, Width, Height / 2 + y);
            }
        }

        private void DrawOverlay(Graphics g)
        {
            string text = string.Format("{0} room(s), {1} object(s)    {2}    ",
                _documents.Count,
                TotalObjectCount(),
                ViewMode == LevelViewMode.TopDown ? "top-down" : "3D orbit");
            text += ViewMode == LevelViewMode.TopDown
                ? "wheel: zoom, right/middle drag: pan"
                : "wheel: zoom, left drag empty space: orbit, right/middle drag: pan";
            if (DragObjects && ViewMode == LevelViewMode.TopDown) text += "    left drag object: move X/Z";
            using (Brush brush = new SolidBrush(Color.FromArgb(220, 230, 230, 230)))
            {
                g.DrawString(text, Font, brush, 12, Height - 26);
            }
        }

        private void DrawCenteredText(Graphics g, string text)
        {
            SizeF size = g.MeasureString(text, Font);
            using (Brush b = new SolidBrush(Color.FromArgb(220, 230, 230, 230)))
            {
                g.DrawString(text, Font, b, (Width - size.Width) / 2, (Height - size.Height) / 2);
            }
        }

        private HitResult HitObject(Point point)
        {
            HitResult result = new HitResult();
            float best = 121;
            ProjectionContext context = CreateProjectionContext(_zoom);
            foreach (SR1LevelDocument doc in _documents)
            {
                for (int i = 0; i < doc.Objects.Count; i++)
                {
                    LevelObjectPlacement obj = doc.Objects[i];
                    PointF p = Project(context, obj.X, obj.Y, obj.Z);
                    float dx = point.X - p.X;
                    float dy = point.Y - p.Y;
                    float dist = dx * dx + dy * dy;
                    if (dist <= best)
                    {
                        best = dist;
                        result.Document = doc;
                        result.ObjectIndex = i;
                    }
                }
            }
            return result;
        }

        private PortalHitResult HitPortal(Point point)
        {
            PortalHitResult result = new PortalHitResult();
            float best = float.MaxValue;
            ProjectionContext context = CreateProjectionContext(_zoom);
            foreach (SR1LevelDocument doc in _documents)
            {
                foreach (LevelPortal portal in doc.Portals)
                {
                    PointF p1 = Project(context, portal.MinX, portal.MinY, portal.MinZ);
                    PointF p2 = Project(context, portal.MaxX, portal.MaxY, portal.MaxZ);
                    RectangleF r = RectFromPoints(p1, p2);
                    if (r.Width < 8) r.Inflate((8 - r.Width) / 2.0f, 0);
                    if (r.Height < 8) r.Inflate(0, (8 - r.Height) / 2.0f);
                    r.Inflate(5, 5);
                    if (!r.Contains(point)) continue;

                    float cx = r.Left + r.Width / 2.0f;
                    float cy = r.Top + r.Height / 2.0f;
                    float dx = point.X - cx;
                    float dy = point.Y - cy;
                    float dist = dx * dx + dy * dy;
                    if (dist < best)
                    {
                        best = dist;
                        result.Document = doc;
                        result.Portal = portal;
                    }
                }
            }
            return result;
        }

        private PointF Project(ProjectionContext context, float x, float y, float z)
        {
            if (ViewMode == LevelViewMode.TopDown) return WorldToScreenTopDown(context, x, z);

            float rx = x - context.CenterX;
            float ry = y - context.CenterY;
            float rz = z - context.CenterZ;

            float x1 = rx * context.CosYaw + rz * context.SinYaw;
            float z1 = -rx * context.SinYaw + rz * context.CosYaw;
            float y1 = ry * context.CosPitch - z1 * context.SinPitch;

            return new PointF(Width / 2 + _pan.X + x1 * context.Scale, Height / 2 + _pan.Y - y1 * context.Scale);
        }

        private PointF WorldToScreenTopDown(ProjectionContext context, float x, float z)
        {
            float sx = (x - context.CenterX) * context.Scale + Width / 2 + _pan.X;
            float sy = -(z - context.CenterZ) * context.Scale + Height / 2 + _pan.Y;
            return new PointF(sx, sy);
        }

        private PointF ScreenToWorldTopDown(ProjectionContext context, Point point)
        {
            float x = (point.X - Width / 2 - _pan.X) / context.Scale + context.CenterX;
            float z = -((point.Y - Height / 2 - _pan.Y) / context.Scale) + context.CenterZ;
            return new PointF(x, z);
        }

        private ProjectionContext CreateProjectionContext(float zoom)
        {
            RectangleF b = SceneBounds();
            float minY, maxY;
            SceneYBounds(out minY, out maxY);
            float baseScale = CalculateBaseScale(b);
            ProjectionContext context = new ProjectionContext();
            context.Bounds = b;
            context.CenterX = b.Left + b.Width / 2.0f;
            context.CenterZ = b.Top + b.Height / 2.0f;
            context.MinY = minY;
            context.MaxY = maxY;
            context.CenterY = (minY + maxY) / 2.0f;
            context.BaseScale = baseScale;
            context.Scale = baseScale * zoom;
            context.CosYaw = (float)Math.Cos(_yaw);
            context.SinYaw = (float)Math.Sin(_yaw);
            context.CosPitch = (float)Math.Cos(_pitch);
            context.SinPitch = (float)Math.Sin(_pitch);
            context.Viewport = new RectangleF(-80, -80, Width + 160, Height + 160);
            context.TotalTriangles = TotalTriangleCount();
            return context;
        }

        private float CalculateBaseScale(RectangleF b)
        {
            float margin = 54;
            float usableWidth = Math.Max(1, Width - margin * 2);
            float usableHeight = Math.Max(1, Height - margin * 2);
            float scaleX = usableWidth / Math.Max(1, b.Width);
            float scaleY = usableHeight / Math.Max(1, b.Height);
            return Math.Max(0.01f, Math.Min(scaleX, scaleY));
        }

        private RectangleF SceneBounds()
        {
            bool any = false;
            float minX = 0, minZ = 0, maxX = 0, maxZ = 0;
            foreach (SR1LevelDocument doc in _documents)
            {
                RectangleF b = doc.Bounds;
                if (!any)
                {
                    minX = b.Left;
                    minZ = b.Top;
                    maxX = b.Right;
                    maxZ = b.Bottom;
                    any = true;
                }
                else
                {
                    if (b.Left < minX) minX = b.Left;
                    if (b.Top < minZ) minZ = b.Top;
                    if (b.Right > maxX) maxX = b.Right;
                    if (b.Bottom > maxZ) maxZ = b.Bottom;
                }
            }
            if (!any) return new RectangleF(-1024, -1024, 2048, 2048);
            return RectangleF.FromLTRB(minX, minZ, maxX, maxZ);
        }

        private float SceneCenterY()
        {
            float minY, maxY;
            SceneYBounds(out minY, out maxY);
            return (minY + maxY) / 2;
        }

        private void SceneYBounds(out float minY, out float maxY)
        {
            bool any = false;
            float low = 0;
            float high = 0;
            Action<float> include = delegate(float y)
            {
                if (!any)
                {
                    low = high = y;
                    any = true;
                }
                else
                {
                    if (y < low) low = y;
                    if (y > high) high = y;
                }
            };

            foreach (SR1LevelDocument doc in _documents)
            {
                foreach (LevelVertex v in doc.Vertices)
                {
                    include(v.Y);
                }
                foreach (LevelObjectPlacement obj in doc.Objects)
                {
                    include(obj.Y);
                }
                foreach (LevelPortal portal in doc.Portals)
                {
                    include(portal.MinY);
                    include(portal.MaxY);
                }
            }

            if (!any)
            {
                low = -512;
                high = 512;
            }
            if (high - low < 32) high = low + 32;
            minY = low;
            maxY = high;
        }

        private int TotalObjectCount()
        {
            int count = 0;
            foreach (SR1LevelDocument doc in _documents) count += doc.Objects.Count;
            return count;
        }

        private int TotalTriangleCount()
        {
            int count = 0;
            foreach (SR1LevelDocument doc in _documents) count += doc.Triangles.Count;
            return count;
        }

        private static RectangleF RectFromPoints(PointF a, PointF b)
        {
            float left = Math.Min(a.X, b.X);
            float top = Math.Min(a.Y, b.Y);
            float right = Math.Max(a.X, b.X);
            float bottom = Math.Max(a.Y, b.Y);
            return RectangleF.FromLTRB(left, top, right, bottom);
        }

        private static bool TriangleNearViewport(PointF a, PointF b, PointF c, RectangleF viewport)
        {
            float left = Math.Min(a.X, Math.Min(b.X, c.X));
            float right = Math.Max(a.X, Math.Max(b.X, c.X));
            float top = Math.Min(a.Y, Math.Min(b.Y, c.Y));
            float bottom = Math.Max(a.Y, Math.Max(b.Y, c.Y));
            return right >= viewport.Left && left <= viewport.Right && bottom >= viewport.Top && top <= viewport.Bottom;
        }

        private static bool SegmentNearViewport(PointF a, PointF b, RectangleF viewport)
        {
            if (a.X < viewport.Left && b.X < viewport.Left) return false;
            if (a.X > viewport.Right && b.X > viewport.Right) return false;
            if (a.Y < viewport.Top && b.Y < viewport.Top) return false;
            if (a.Y > viewport.Bottom && b.Y > viewport.Bottom) return false;
            return true;
        }

        private static bool PointNearViewport(PointF p, RectangleF viewport, float margin)
        {
            return p.X >= viewport.Left - margin &&
                p.X <= viewport.Right + margin &&
                p.Y >= viewport.Top - margin &&
                p.Y <= viewport.Bottom + margin;
        }

        private static short ClampShort(float value)
        {
            if (value < short.MinValue) return short.MinValue;
            if (value > short.MaxValue) return short.MaxValue;
            return (short)Math.Round(value);
        }

        private static short AddShortDelta(short value, int delta)
        {
            int next = value + delta;
            if (next < short.MinValue) return short.MinValue;
            if (next > short.MaxValue) return short.MaxValue;
            return (short)next;
        }

        private static Color RoomColor(int index, int alpha)
        {
            Color[] colors =
            {
                Color.FromArgb(alpha, 90, 190, 190),
                Color.FromArgb(alpha, 190, 160, 90),
                Color.FromArgb(alpha, 150, 130, 220),
                Color.FromArgb(alpha, 120, 190, 120),
                Color.FromArgb(alpha, 220, 120, 150)
            };
            return colors[Math.Abs(index) % colors.Length];
        }

        private static Color ObjectColor(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return Color.FromArgb(255, 120, 190, 255);
            string lower = fileName.ToLowerInvariant();
            if (lower.Contains("soul") || lower.Contains("reavr") || lower.Contains("raziel")) return Color.FromArgb(255, 120, 220, 255);
            if (lower.Contains("vamp") || lower.Contains("sluagh") || lower.Contains("wraith") || lower.Contains("human")) return Color.FromArgb(255, 255, 120, 120);
            if (lower.Contains("swd") || lower.Contains("trch") || lower.Contains("weap")) return Color.FromArgb(255, 245, 190, 90);
            return Color.FromArgb(255, 120, 220, 150);
        }

        private struct HitResult
        {
            public SR1LevelDocument Document;
            public int ObjectIndex;
        }

        private struct PortalHitResult
        {
            public SR1LevelDocument Document;
            public LevelPortal Portal;
        }

        private enum ViewDragMode
        {
            None,
            Pan,
            Orbit
        }

        private struct TerrainEdge
        {
            public int A;
            public int B;
        }

        private sealed class ProjectionContext
        {
            public RectangleF Bounds;
            public RectangleF Viewport;
            public float CenterX;
            public float CenterY;
            public float CenterZ;
            public float MinY;
            public float MaxY;
            public float BaseScale;
            public float Scale;
            public float CosYaw;
            public float SinYaw;
            public float CosPitch;
            public float SinPitch;
            public int TotalTriangles;
        }
    }

    internal sealed class PortalActivatedEventArgs : EventArgs
    {
        public readonly SR1LevelDocument Document;
        public readonly LevelPortal Portal;

        public PortalActivatedEventArgs(SR1LevelDocument document, LevelPortal portal)
        {
            Document = document;
            Portal = portal;
        }
    }
}
