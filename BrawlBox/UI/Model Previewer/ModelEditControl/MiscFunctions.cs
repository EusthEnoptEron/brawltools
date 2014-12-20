﻿using BrawlLib.Imaging;
using BrawlLib.Modeling;
using BrawlLib.SSBB.ResourceNodes;
using BrawlLib.SSBBTypes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace System.Windows.Forms
{
    public partial class ModelEditControl : ModelEditorBase
    {
        public override void UpdatePropDisplay()
        {
            if (vertexEditor.Visible)
            {
                vertexEditor.UpdatePropDisplay();
                return;
            }
            base.UpdatePropDisplay();
        }

        public override void AppendTarget(IModel model)
        {
            if (!_targetModels.Contains(model))
                _targetModels.Add(model);

            if (!models.Items.Contains(model))
                models.Items.Add(model);

            ModelPanel.AddTarget(model);
            model.ResetToBindState();
        }

        private void cboToolSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            _updating = true;
            switch (ControlType)
            {
                case TransformType.None:
                    rotationToolStripMenuItem.Checked = 
                    translationToolStripMenuItem.Checked = 
                    scaleToolStripMenuItem.Checked = false;
                    break;
                case TransformType.Scale:
                    rotationToolStripMenuItem.Checked = 
                    translationToolStripMenuItem.Checked = false;
                    scaleToolStripMenuItem.Checked = true;
                    break;
                case TransformType.Rotation:
                    translationToolStripMenuItem.Checked = 
                    scaleToolStripMenuItem.Checked = false;
                    rotationToolStripMenuItem.Checked = true;
                    break;
                case TransformType.Translation:
                    rotationToolStripMenuItem.Checked =
                    scaleToolStripMenuItem.Checked = false;
                    translationToolStripMenuItem.Checked = true;
                    break;
            }
            _updating = false;

            _snapCirc = _snapX = _snapY = _snapZ = false;
            ModelPanel.Invalidate();
        }

        protected override void OnModelChanged()
        {
            _updating = true;
            if (_targetModel != null && !EditingAll && TargetCollision == null)
                models.SelectedItem = _targetModel;

            leftPanel.Reset();
            rightPanel.Reset();

            _updating = false;
        }

        private void models_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_updating)
                return;

            EditingAll = models.SelectedIndex == 0;

            //Leave the target model and collision alone if just switching to edit all
            if (!EditingAll)
            {
                _resetCamera = false;
                TargetModel = models.SelectedItem is IModel ? (IModel)models.SelectedItem : null;
                TargetCollision = models.SelectedItem is CollisionNode ? (CollisionNode)models.SelectedItem : null;
            }
            _undoSaves.Clear();
            _redoSaves.Clear();
            _saveIndex = -1;
        }

        bool addedHeight = false;
        private void ModelEditControl_SizeChanged(object sender, EventArgs e)
        {
            CheckDimensions();
        }

        public void CheckDimensions()
        {
            if (pnlPlayback.Width <= pnlPlayback.MinimumSize.Width)
            {
                pnlPlayback.Dock = DockStyle.Left;
                pnlPlayback.Width = pnlPlayback.MinimumSize.Width;
            }
            else
                pnlPlayback.Dock = DockStyle.Fill;

            if (_updating)
                return;

            if (animEditors.Width - animCtrlPnl.Width >= pnlPlayback.MinimumSize.Width)
            {
                pnlPlayback.Width += animEditors.Width - animCtrlPnl.Width - pnlPlayback.MinimumSize.Width;
                pnlPlayback.Dock = DockStyle.Fill;
            }
            else pnlPlayback.Dock = DockStyle.Left;

            if (animCtrlPnl.Width + pnlPlayback.Width <= animEditors.Width)
            {
                if (addedHeight)
                {
                    _updating = true;
                    animEditors.Height -= 17;
                    _updating = false;
                    animEditors.HorizontalScroll.Visible = addedHeight = false;
                }
            }
            else
            {
                if (!addedHeight)
                {
                    _updating = true;
                    animEditors.Height += 17;
                    _updating = false;
                    animEditors.HorizontalScroll.Visible = addedHeight = true;
                }
            }
        }

        public bool Close()
        {
            try
            {
                if (!CloseExternal())
                    return false;

                SaveSettings();

                StopAnim();

                if (_viewerForm != null)
                    _viewerForm.Close();
                if (_interpolationForm != null)
                    _interpolationForm.Close();

                ResetBoneColors();

                MDL0TextureNode._folderWatcher.SynchronizingObject = null;
            }
            catch { }
            return true;
        }

        protected override void OnSelectedBoneChanged()
        {
            //weightEditor.BoneChanged();
            chkZoomExtents.Enabled = AllowZoomExtents;
        }
        public override void UpdateUndoButtons()
        {
            btnUndo.Enabled = CanUndo;
            btnRedo.Enabled = CanRedo;
        }
        protected override void OnSelectedVerticesChanged()
        {
            base.OnSelectedVerticesChanged();

            //weightEditor.TargetVertices = _selectedVertices;
            vertexEditor._targetVertices = _selectedVertices;
        }

        public override void UpdateAnimationPanelDimensions()
        {
            if (_currentControl is SCN0Editor)
            {
                int x, y, z;
                scn0Editor.GetDimensions(out x, out y, out z);
                animEditors.Height = x;
                animCtrlPnl.Height = y;
                animCtrlPnl.Width = z;
            }
        }

        public void SetCurrentControl()
        {
            Control newControl = null;
            syncTexObjToolStripMenuItem.Checked = (TargetAnimType == NW4RAnimType.SRT || TargetAnimType == NW4RAnimType.PAT);
            switch (TargetAnimType)
            {
                case NW4RAnimType.CHR: newControl = chr0Editor; break;
                case NW4RAnimType.SHP: newControl = shp0Editor; break;
                case NW4RAnimType.VIS: newControl = vis0Editor; break;
                case NW4RAnimType.SCN: newControl = scn0Editor; break;
                case NW4RAnimType.CLR: newControl = clr0Editor; break;
                case NW4RAnimType.SRT: newControl = srt0Editor; break;
                case NW4RAnimType.PAT: newControl = pat0Editor; break;
            }
            if (_currentControl != newControl)
            {
                if (_currentControl != null)
                    _currentControl.Visible = false;
                _currentControl = newControl;

                if (!(_currentControl is SRT0Editor) && !(_currentControl is PAT0Editor))
                    syncTexObjToolStripMenuItem.Checked = false;

                if (_currentControl != null)
                {
                    _currentControl.Visible = true;
                    if (_currentControl is CHR0Editor)
                    {
                        animEditors.Height = 78;
                        animCtrlPnl.Width = 582;
                    }
                    else if (_currentControl is SRT0Editor)
                    {
                        animEditors.Height = 78;
                        animCtrlPnl.Width = 483;
                    }
                    else if (_currentControl is SHP0Editor)
                    {
                        animEditors.Height = 106;
                        animCtrlPnl.Width = 533;
                    }
                    else if (_currentControl is PAT0Editor)
                    {
                        animEditors.Height = 78;
                        animCtrlPnl.Width = 402;
                    }
                    else if (_currentControl is VIS0Editor)
                    {
                        animEditors.Height = 62;
                        animCtrlPnl.Width = 210;
                    }
                    else if (_currentControl is CLR0Editor)
                    {
                        animEditors.Height = 62;
                        animCtrlPnl.Width = 168;
                    }
                    else if (_currentControl is SCN0Editor)
                    {
                        int x, y, z;
                        scn0Editor.GetDimensions(out x, out y, out z);
                        animEditors.Height = x;
                        animCtrlPnl.Height = y;
                        animCtrlPnl.Width = z;
                    }
                    else
                        animEditors.Height = animCtrlPnl.Width = 0;
                }
                else animEditors.Height = animCtrlPnl.Width = 0;
                return;
            }
            CheckDimensions();
            UpdatePropDisplay();
        }

        protected override void UpdateSRT0FocusControls(SRT0Node node) { leftPanel.UpdateSRT0Selection(node); }
        protected override void UpdatePAT0FocusControls(PAT0Node node) { leftPanel.UpdatePAT0Selection(node); }

        protected override void modelPanel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                ModelPanel.AllowSelection = true;

                if (_rotating || _translating || _scaling)
                    if (VertexLoc() == null)
                    {
                        BoneChange(SelectedBone);
                        if (chkSnapToColl.Checked)
                            SnapYIfClose();
                    }
                    else
                        VertexChange(_selectedVertices);

                _snapX = _snapY = _snapZ = _snapCirc = false;
                _rotating = _translating = _scaling = false;

                //if (weightEditor.TargetVertices != _selectedVertices)
                //    weightEditor.TargetVertices = _selectedVertices;
                if (vertexEditor.TargetVertices != _selectedVertices)
                    vertexEditor.TargetVertices = _selectedVertices;
            }
        }

        public override void ApplyVIS0ToInterface()
        {
            if (_animFrame == 0 || leftPanel.lstObjects.Items.Count == 0)
                return;

            VIS0Updating = true;
            if (_vis0 != null)
            {
                //if (TargetAnimation != null && _vis0.FrameCount != TargetAnimation.tFrameCount)
                //    UpdateVis0(null, null);

                foreach (string n in VIS0Indices.Keys)
                {
                    VIS0EntryNode node = null;
                    List<int> indices = VIS0Indices[n];
                    for (int i = 0; i < indices.Count; i++)
                    {
                        if ((node = (VIS0EntryNode)_vis0.FindChild(((MDL0ObjectNode)leftPanel.lstObjects.Items[indices[i]])._bone.Name, true)) != null)
                        {
                            if (node._entryCount != 0 && _animFrame > 0)
                                leftPanel.lstObjects.SetItemChecked(indices[i], node.GetEntry((int)_animFrame - 1));
                            else
                                leftPanel.lstObjects.SetItemChecked(indices[i], node._flags.HasFlag(VIS0Flags.Enabled));
                        }
                    }
                }
            }
            VIS0Updating = false;
        }

        #region Hotkeys
        private bool HotkeySelectAllVertices()
        {
            if (!ModelPanel.Focused)
                return false;

            ResetVertexColors();
            if (_targetModels != null)
                foreach (IModel mdl in _targetModels)
                    if (mdl.SelectedObjectIndex >= 0 && mdl.SelectedObjectIndex < mdl.Objects.Length)
                        foreach (Vertex3 v in ((IObject)mdl.Objects[mdl.SelectedObjectIndex]).PrimitiveManager._vertices)
                        {
                            _selectedVertices.Add(v);
                            v._selected = true;
                            v._highlightColor = Color.Orange;
                        }
                    else
                        foreach (IObject o in mdl.Objects)
                            foreach (Vertex3 v in o.PrimitiveManager._vertices)
                            {
                                _selectedVertices.Add(v);
                                v._selected = true;
                                v._highlightColor = Color.Orange;
                            }

            OnSelectedVerticesChanged();
            ModelPanel.Invalidate();

            return true;
        }
        private bool HotkeyToggleLeftPanel()
        {
            if (ModelPanel.Focused)
            {
                btnLeftToggle_Click(this, EventArgs.Empty);
                return true;
            }
            return false;
        }
        private bool HotkeyToggleTopPanel()
        {
            if (ModelPanel.Focused)
            {
                btnTopToggle_Click(this, EventArgs.Empty);
                return true;
            }
            return false;
        }
        private bool HotkeyToggleRightPanel()
        {
            if (ModelPanel.Focused)
            {
                btnRightToggle_Click(this, EventArgs.Empty);
                return true;
            }
            return false;
        }
        private bool HotkeyToggleBottomPanel()
        {
            if (ModelPanel.Focused)
            {
                btnBottomToggle_Click(this, EventArgs.Empty);
                return true;
            }
            return false;
        }
        private bool HotkeyToggleAllPanels()
        {
            if (ModelPanel.Focused)
            {
                if (leftPanel.Visible || rightPanel.Visible || animEditors.Visible || controlPanel.Visible)
                    showBottom.Checked = showRight.Checked = showLeft.Checked = showTop.Checked = false;
                else
                    showBottom.Checked = showRight.Checked = showLeft.Checked = showTop.Checked = true;
                return true;
            }
            return false;
        }
        private bool HotkeyScaleTool()
        {
            if (ModelPanel.Focused)
            {
                ControlType = TransformType.Scale;
                return true;
            }
            return false;
        }
        private bool HotkeyRotateTool()
        {
            if (ModelPanel.Focused)
            {
                ControlType = TransformType.Rotation;
                return true;
            }
            return false;
        }
        private bool HotkeyTranslateTool()
        {
            if (ModelPanel.Focused)
            {
                ControlType = TransformType.Translation;
                return true;
            }
            return false;
        }

        private bool HotkeyWeightEditor()
        {
            if (ModelPanel.Focused)
            {
                ToggleWeightEditor();
                return true;
            }
            return false;
        }
        private bool HotkeyVertexEditor()
        {
            if (ModelPanel.Focused)
            {
                ToggleVertexEditor();
                return true;
            }
            return false;
        }

        public override void InitHotkeyList()
        {
            base.InitHotkeyList();

            List<HotKeyInfo> temp = new List<HotKeyInfo>()
            {
                new HotKeyInfo(Keys.A, true, false, false, HotkeySelectAllVertices),
                new HotKeyInfo(Keys.A, false, false, false, HotkeyToggleLeftPanel),
                new HotKeyInfo(Keys.D, false, false, false, HotkeyToggleRightPanel),
                new HotKeyInfo(Keys.W, false, false, false, HotkeyToggleTopPanel),
                new HotKeyInfo(Keys.S, false, false, false, HotkeyToggleBottomPanel),
                new HotKeyInfo(Keys.D, true, true, false, HotkeyToggleAllPanels),
                new HotKeyInfo(Keys.E, false, false, false, HotkeyScaleTool),
                new HotKeyInfo(Keys.R, false, false, false, HotkeyRotateTool),
                new HotKeyInfo(Keys.T, false, false, false, HotkeyTranslateTool),
                new HotKeyInfo(Keys.J, false, false, false, HotkeyVertexEditor),

                //Weight editor has been disabled due to the necessity
                //of re-encoding objects after making influence changes.
                //new HotKeyInfo(Keys.H, false, false, false, HotkeyWeightEditor),
            };
            _hotkeyList.AddRange(temp);
        }
        #endregion

        #region Collisions
        private bool PointCollides(Vector3 point) {
            float f;
            return PointCollides(point, out f);
        }
        private bool PointCollides(Vector3 point, out float y_result) {
            y_result = float.MaxValue;
            Vector2 v2 = new Vector2(point._x, point._y);
            foreach (CollisionNode coll in _collisions) {
                foreach (CollisionObject obj in coll._objects) {
                    if (obj._render) {
                        foreach (CollisionPlane plane in obj._planes) {
                            if (plane._type == BrawlLib.SSBBTypes.CollisionPlaneType.Floor) {
                                if (plane.PointLeft._x < v2._x && plane.PointRight._x > v2._x) {
                                    float x = v2._x;
                                    float m = (plane.PointLeft._y - plane.PointRight._y)
                                        / (plane.PointLeft._x - plane.PointRight._x);
                                    float b = plane.PointRight._y - m * plane.PointRight._x;
                                    float y_target = m * x + b;
                                    Helpers.DbgPrint(y_target.ToString());
                                    if (Math.Abs(y_target - v2._y) <= Math.Abs(y_result - v2._y)) {
                                        y_result = y_target;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return (Math.Abs(y_result - v2._y) <= 5);
        }
        private void SnapYIfClose() {
            float f;
            if (PointCollides(new Vector3(chr0Editor._transBoxes[6].Value, chr0Editor._transBoxes[7].Value, chr0Editor._transBoxes[8].Value), out f)) {
                ApplyTranslation(1, f - chr0Editor._transBoxes[7].Value);
            }
        }
        #endregion

        #region Settings
        public override void SaveSettings()
        {
            BrawlBox.Properties.Settings.Default.ViewerSettings = CollectSettings();
            BrawlBox.Properties.Settings.Default.ScreenCapBgLocText = ScreenCapBgLocText.Text;
            BrawlBox.Properties.Settings.Default.LiveTextureFolderPath = LiveTextureFolderPath.Text;
            BrawlBox.Properties.Settings.Default.ViewerSettingsSet = true;
            BrawlBox.Properties.Settings.Default.Save();
        }
        public BrawlBoxViewerSettings CollectSettings()
        {
            BrawlBoxViewerSettings settings = new BrawlBoxViewerSettings();
            settings._tag = BrawlBoxViewerSettings.Tag;
            settings._version = 5;

            settings.RetrieveCorrAnims = syncAnimationsTogetherToolStripMenuItem.Checked;
            settings.DisplayExternalAnims = chkExternalAnims.Checked;
            settings.DisplayBRRESAnims = chkBRRESAnims.Checked;
            settings.DisplayNonBRRESAnims = chkNonBRRESAnims.Checked;
            settings.SyncTexToObj = syncTexObjToolStripMenuItem.Checked;
            settings.SyncObjToVIS0 = syncObjectsListToVIS0ToolStripMenuItem.Checked;
            settings.DisableBonesOnPlay = disableBonesWhenPlayingToolStripMenuItem.Checked;

            settings.RightPanelWidth = (uint)rightPanel.Width;
            //settings._defaultCam = ModelPanel.DefaultTranslate;
            //settings._defaultRot = ModelPanel.DefaultRotate;
            //settings._amb = ModelPanel.Ambient;
            //settings._pos = ModelPanel.LightPosition;
            //settings._diff = ModelPanel.Diffuse;
            //settings._spec = ModelPanel.Specular;
            //settings._yFov = ModelPanel._fovY;
            //settings._nearZ = ModelPanel._nearZ;
            //settings._farz = ModelPanel._farZ;
            //settings._tScale = ModelPanel.TranslationScale;
            //settings._rScale = ModelPanel.RotationScale;
            //settings._zScale = ModelPanel.ZoomScale;
            //settings._orbColor = (ARGBPixel)MDL0BoneNode.DefaultNodeColor;
            //settings._lineColor = (ARGBPixel)MDL0BoneNode.DefaultBoneColor;
            //settings._floorColor = (ARGBPixel)StaticMainWindow._floorHue;
            settings._undoCount = (uint)_allowedUndos;
            settings._shaderCount = 0;
            settings._matCount = 0;
            //settings._emis = ModelPanel.Emission;

            settings.FlatBoneList = rightPanel.pnlBones.chkFlat.Checked;
            settings.BoneListContains = rightPanel.pnlBones.chkContains.Checked;
            settings.SnapToColl = chkSnapToColl.Checked;
            settings.Maximize = chkMaximize.Checked;
            //settings.CameraSet = btnSaveCam.Text == "Clear Camera";
            settings.ImageCapFmt = _imgExtIndex;

            //settings.Bones = _renderBones;
            //settings.Polys = _renderPolygons;
            //settings.Wireframe = _renderWireframe;
            //settings.Vertices = _renderVertices;
            //settings.Normals = _renderNormals;
            //settings.HideOffscreen = _dontRenderOffscreen;
            //settings.BoundingBox = _renderBox;
            //settings.Floor = _renderFloor;

            //settings.ShowCamCoords = showCameraCoordinatesToolStripMenuItem.Checked;
            //settings.OrthoCam = orthographicToolStripMenuItem.Checked;
            //settings.EnableSmoothing = enablePointAndLineSmoothingToolStripMenuItem.Checked;
            //settings.EnableText = enableTextOverlaysToolStripMenuItem.Checked;

            settings.LinearCHR = chkLinearCHR.Checked;
            settings.LinearSRT = chkLinearSRT.Checked;
            settings.LinearSHP = chkLinearSHP.Checked;
            settings.LinearLight = chkLinearLight.Checked;
            settings.LinearFog = chkLinearFog.Checked;
            settings.LinearCam = chkLinearCamera.Checked;

            settings.GenTansCHR = chkGenTansCHR.Checked;
            settings.GenTansSRT = chkGenTansSRT.Checked;
            settings.GenTansSHP = chkGenTansSHP.Checked;
            settings.GenTansLight = chkGenTansLight.Checked;
            settings.GenTansFog = chkGenTansFog.Checked;
            settings.GenTansCam = chkGenTansCamera.Checked;

            return settings;
        }
        public override void SetDefaultSettings() { DistributeSettings(BrawlBoxViewerSettings.Default); }
        public void DistributeSettings(BrawlBoxViewerSettings settings)
        {
            _updating = true;
            ModelPanel.BeginUpdate();

            syncAnimationsTogetherToolStripMenuItem.Checked = settings.RetrieveCorrAnims;
            syncTexObjToolStripMenuItem.Checked = settings.SyncTexToObj;
            syncObjectsListToVIS0ToolStripMenuItem.Checked = settings.SyncObjToVIS0;
            disableBonesWhenPlayingToolStripMenuItem.Checked = settings.DisableBonesOnPlay;
            chkSnapToColl.Checked = settings.SnapToColl;
            chkMaximize.Checked = settings.Maximize;
            chkExternalAnims.Checked = settings.DisplayExternalAnims;
            chkBRRESAnims.Checked = settings.DisplayBRRESAnims;
            chkNonBRRESAnims.Checked = settings.DisplayNonBRRESAnims;
            rightPanel.pnlBones.chkFlat.Checked = settings.FlatBoneList;
            rightPanel.pnlBones.chkContains.Checked = settings.BoneListContains;

            int w = (int)settings.RightPanelWidth;
            if (w >= 50)
                rightPanel.Width = w;

            //ModelPanel.Ambient = settings._amb;
            //ModelPanel.LightPosition = settings._pos;
            //ModelPanel.Diffuse = settings._diff;
            //ModelPanel.Specular = settings._spec;
            //ModelPanel.Emission = settings._emis;

            //ModelPanel._fovY = settings._yFov;
            //ModelPanel._nearZ = settings._nearZ;
            //ModelPanel._farZ = settings._farz;

            //ModelPanel.ZoomScale = settings._zScale;
            //ModelPanel.TranslationScale = settings._tScale;
            //ModelPanel.RotationScale = settings._rScale;

            //MDL0BoneNode.DefaultNodeColor = (Color)settings._orbColor;
            //MDL0BoneNode.DefaultBoneColor = (Color)settings._lineColor;
            //StaticMainWindow._floorHue = (Color)settings._floorColor;
            //if (settings.CameraSet)
            //{
            //    btnSaveCam.Text = "Clear Camera";
            //    ModelPanel.DefaultTranslate = settings._defaultCam;
            //    ModelPanel.DefaultRotate = settings._defaultRot;
            //}
            //else
            //{
            //    btnSaveCam.Text = "Save Camera";
            //    ModelPanel.DefaultTranslate = new Vector3();
            //    ModelPanel.DefaultRotate = new Vector2();
            //}

            _allowedUndos = settings._undoCount;
            ImgExtIndex = settings.ImageCapFmt;

            //RenderBones = settings.Bones;
            //RenderWireframe = settings.Wireframe;
            //RenderPolygons = settings.Polys;
            //RenderVertices = settings.Vertices;
            //RenderBox = settings.BoundingBox;
            //RenderNormals = settings.Normals;
            //DontRenderOffscreen = settings.HideOffscreen;
            //RenderFloor = settings.Floor;

            //showCameraCoordinatesToolStripMenuItem.Checked = settings.ShowCamCoords;
            //enablePointAndLineSmoothingToolStripMenuItem.Checked = settings.EnableSmoothing;
            //enableTextOverlaysToolStripMenuItem.Checked = settings.EnableText;

            chkLinearCHR.Checked = settings.LinearCHR;
            chkLinearSRT.Checked = settings.LinearSRT;
            chkLinearSHP.Checked = settings.LinearSHP;
            chkLinearLight.Checked = settings.LinearLight;
            chkLinearFog.Checked = settings.LinearFog;
            chkLinearCamera.Checked = settings.LinearCam;

            chkGenTansCHR.Checked = settings.GenTansCHR;
            chkGenTansSRT.Checked = settings.GenTansSRT;
            chkGenTansSHP.Checked = settings.GenTansSHP;
            chkGenTansLight.Checked = settings.GenTansLight;
            chkGenTansFog.Checked = settings.GenTansFog;
            chkGenTansCamera.Checked = settings.GenTansCam;

            ModelPanel.EndUpdate();
            _updating = false;
        }
        #endregion
    }
}
