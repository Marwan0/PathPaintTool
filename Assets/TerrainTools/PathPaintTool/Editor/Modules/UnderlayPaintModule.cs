﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;


namespace UnityEditor.Experimental.TerrainAPI
{
    public class UnderlayModule : ModuleEditor
    {
        #region Fields

        [SerializeField]
        TerrainLayer m_SelectedInnerTerrainLayer = null;

        [SerializeField]
        int innerLayerIndex = -1;

        [SerializeField]
        float paintBrushSize = 140f;

        [SerializeField]
        float paintBrushStrength = 100;

        #endregion Fields

        private Color paintBrushColor = new Color(1.0f, 0.6f, 0, 0.6f);

        public UnderlayModule(bool active, int sceneGuiOrder, int paintSegmentOrder) : base(active, sceneGuiOrder, paintSegmentOrder)
        {
        }

        override public string GetName()
        {
            return "Underlay";
        }

        override public string GetDescription()
        {
            return "";
        }

        override public void OnSceneGUI(Terrain currentTerrain, IOnSceneGUI editContext)
        {
            if (editContext.hitValidTerrain)
            {
                Terrain terrain = currentTerrain;

                // the smooth brush size is relative to the main brush size
                float brushSize = editContext.brushSize * paintBrushSize / 100f;

                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, brushSize, 0.0f);
                PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);
                Material brushPreviewMat = BrushUtilities.GetDefaultBrushPreviewMaterial();
                brushPreviewMat.color = paintBrushColor;
                BrushUtilities.DrawBrushPreview(ctx, BrushUtilities.BrushPreview.SourceRenderTexture, editContext.brushTexture, brushXform, brushPreviewMat, 0);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }

        override public void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            innerLayerIndex = LayerUtilities.ShowTerrainLayersSelection("Underlay", terrain, innerLayerIndex);
            m_SelectedInnerTerrainLayer = LayerUtilities.FindTerrainLayer(terrain, innerLayerIndex);

            paintBrushSize = EditorGUILayout.Slider(new GUIContent("Brush Size [% of Main Brush]", ""), paintBrushSize, 0.0f, 200.0f);

            paintBrushStrength = EditorGUILayout.Slider(new GUIContent("Brush Strength", ""), paintBrushStrength, 0.0f, 100.0f);


            EditorGUILayout.Space();

            if (innerLayerIndex == -1)
            {
                EditorGUILayout.HelpBox(PathPaintStyles.noTextureSelectedContent.text, MessageType.Warning);
            }

        }

        override public void PaintSegments(StrokeSegment[] segments, IOnPaint editContext)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                StrokeSegment segment = segments[i];

                PaintTexture(segment.currTerrain, editContext, segment.currUV);
            }
        }

        private bool PaintTexture(Terrain terrain, IOnPaint editContext, Vector2 currUV)
        {
            // the brush size is relative to the main brush size
            float brushSize = editContext.brushSize * paintBrushSize / 100f;

            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, currUV, brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintTexture(terrain, brushXform.GetBrushXYBounds(), m_SelectedInnerTerrainLayer);

            if (paintContext == null)
                return false;


            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            float targetAlpha = 1.0f;       // always 1.0 now -- no subtractive painting (we assume this in the ScatterAlphaMap)
            float brushStrength = paintBrushStrength / 100f; // editContext.brushStrength

            // apply brush
            Vector4 brushParams = new Vector4(brushStrength, targetAlpha, 0.0f, 0.0f);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);

            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.PaintTexture);

            TerrainPaintUtility.EndPaintTexture(paintContext, "Terrain Paint - Texture");

            return true;
        }
    }
}
