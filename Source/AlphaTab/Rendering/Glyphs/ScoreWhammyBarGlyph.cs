﻿using AlphaTab.Model;
using AlphaTab.Platform;
using AlphaTab.Platform.Model;
using AlphaTab.Rendering.Utils;

namespace AlphaTab.Rendering.Glyphs
{
    public class ScoreWhammyBarGlyph : ScoreHelperNotesBaseGlyph
    {
        public const int SimpleDipHeight = TabWhammyBarGlyph.PerHalfSize * 2;
        public const int SimpleDipPadding = 2;
        private readonly Beat _beat;

        public ScoreWhammyBarGlyph(Beat beat)
        {
            _beat = beat;
        }

        public override void DoLayout()
        {
            var whammyMode = Renderer.Settings.WhammyMode;
            switch (_beat.WhammyBarType)
            {
                case WhammyType.None:
                case WhammyType.Custom:
                case WhammyType.Hold:
                    return;
                case WhammyType.Dive:
                case WhammyType.PrediveDive:
                    {
                        var endGlyphs = new BendNoteHeadGroupGlyph();
                        endGlyphs.Renderer = Renderer;

                        var lastWhammyPoint = _beat.WhammyBarPoints[_beat.WhammyBarPoints.Count - 1];
                        foreach (var note in _beat.Notes)
                        {
                            if (!note.IsTieOrigin)
                            {
                                endGlyphs.AddGlyph(GetBendNoteValue(note, lastWhammyPoint), (lastWhammyPoint.Value % 2) != 0);
                            }
                        }

                        endGlyphs.DoLayout();
                        _bendNoteHeads.Add(endGlyphs);
                    }
                    break;
                case WhammyType.Dip:
                    {
                        if (whammyMode == WhammyMode.SongBook)
                        {
                            var res = Renderer.Resources;
                            ((ScoreBarRenderer)Renderer).SimpleWhammyOverflow =
                                res.TablatureFont.Size * 1.5f + SimpleDipHeight * Scale + 2*Scale;
                        }
                        else
                        {
                            var middleGlyphs = new BendNoteHeadGroupGlyph();
                            middleGlyphs.Renderer = Renderer;
                            if (Renderer.Settings.WhammyMode == WhammyMode.GuitarPro)
                            {
                                var middleBendPoint = _beat.WhammyBarPoints[1];
                                foreach (var note in _beat.Notes)
                                {
                                    middleGlyphs.AddGlyph(GetBendNoteValue(note, _beat.WhammyBarPoints[1]), (middleBendPoint.Value % 2) != 0);
                                }
                            }

                            middleGlyphs.DoLayout();
                            _bendNoteHeads.Add(middleGlyphs);

                            var endGlyphs = new BendNoteHeadGroupGlyph();
                            endGlyphs.Renderer = Renderer;
                            if (Renderer.Settings.WhammyMode == WhammyMode.GuitarPro)
                            {
                                var lastBendPoint = _beat.WhammyBarPoints[_beat.WhammyBarPoints.Count - 1];
                                foreach (var note in _beat.Notes)
                                {
                                    endGlyphs.AddGlyph(GetBendNoteValue(note, lastBendPoint), (lastBendPoint.Value % 2) != 0);
                                }
                            }
                            endGlyphs.DoLayout();

                            _bendNoteHeads.Add(endGlyphs);
                        }
                    }
                    break;
                case WhammyType.Predive:
                    break;
            }
            base.DoLayout();
        }

        public override void Paint(float cx, float cy, ICanvas canvas)
        {
            var beat = _beat;
            switch (beat.WhammyBarType)
            {
                case WhammyType.None:
                case WhammyType.Custom:
                case WhammyType.Hold:
                    return;
            }

            var whammyMode = Renderer.Settings.WhammyMode;
            var startNoteRenderer = (ScoreBarRenderer)Renderer.ScoreRenderer.Layout.GetRendererForBar(Renderer.Staff.StaveId, beat.Voice.Bar);
            var startX = cx + startNoteRenderer.X + startNoteRenderer.GetBeatX(beat, BeatXPosition.MiddleNotes);
            var beatDirection = GetBeamDirection(beat, startNoteRenderer);
            var direction = _beat.Notes.Count == 1 ? beatDirection : BeamDirection.Up;

            var textalign = canvas.TextAlign;
            canvas.TextAlign = TextAlign.Center;
            for (var i = 0; i < beat.Notes.Count; i++)
            {
                var note = beat.Notes[i];
                var startY = cy + startNoteRenderer.Y + startNoteRenderer.GetNoteY(note, true);
                if (direction == BeamDirection.Down)
                {
                    startY += NoteHeadGlyph.NoteHeadHeight * Scale;
                }

                if (i > 0 && i >= _beat.Notes.Count / 2)
                {
                    direction = BeamDirection.Down;
                }

                var endX = cx + startNoteRenderer.X;
                if (beat.Index == beat.Voice.Beats.Count - 1)
                {
                    endX += startNoteRenderer.Width;
                }
                else
                {
                    endX += startNoteRenderer.GetBeatX(beat, BeatXPosition.EndBeat);
                }

                endX -= EndPadding * Scale;

                ScoreBarRenderer endNoteRenderer = null;
                if (note.IsTieOrigin)
                {
                    endNoteRenderer =
                        (ScoreBarRenderer)Renderer.ScoreRenderer.Layout.GetRendererForBar(Renderer.Staff.StaveId,
                            note.TieDestination.Beat.Voice.Bar);
                    if (endNoteRenderer != null && endNoteRenderer.Staff == startNoteRenderer.Staff)
                    {
                        endX = cx + endNoteRenderer.X +
                               endNoteRenderer.GetBeatX(note.TieDestination.Beat, BeatXPosition.MiddleNotes);
                    }
                    else
                    {
                        endNoteRenderer = null;
                    }
                }

                var heightOffset = (NoteHeadGlyph.NoteHeadHeight * Scale * NoteHeadGlyph.GraceScale) * 0.5f;
                if (direction == BeamDirection.Up) heightOffset = -heightOffset;
                int endValue;
                float endY;

                switch (beat.WhammyBarType)
                {
                    case WhammyType.Dive:
                        if (i == 0)
                        {
                            _bendNoteHeads[0].X = endX - _bendNoteHeads[0].NoteHeadOffset;
                            _bendNoteHeads[0].Y = cy + startNoteRenderer.Y;
                            _bendNoteHeads[0].Paint(0, 0, canvas);
                        }

                        endValue = GetBendNoteValue(note, beat.WhammyBarPoints[beat.WhammyBarPoints.Count - 1]);
                        if (_bendNoteHeads[0].ContainsNoteValue(endValue))
                        {
                            endY = _bendNoteHeads[0].GetNoteValueY(endValue) + heightOffset;
                            DrawBendSlur(canvas, startX, startY, endX, endY, direction == BeamDirection.Down, Scale);
                        }
                        else if (endNoteRenderer != null && (note.IsTieOrigin && note.TieDestination.Beat.HasWhammyBar || (note.Beat.IsContinuedWhammy)))
                        {
                            endY = cy + endNoteRenderer.Y + endNoteRenderer.GetNoteY(note.TieDestination, true);
                            DrawBendSlur(canvas, startX, startY, endX, endY, direction == BeamDirection.Down, Scale);
                        }
                        else if (note.IsTieOrigin)
                        {
                            if (endNoteRenderer == null)
                            {
                                endY = startY;
                            }
                            else
                            {
                                endY = cy + endNoteRenderer.Y + endNoteRenderer.GetNoteY(note.TieDestination, true);
                            }
                            TieGlyph.PaintTie(canvas, Scale, startX, startY, endX, endY,
                                beatDirection == BeamDirection.Down);
                            canvas.Fill();
                        }
                        break;
                    case WhammyType.Dip:
                        if (whammyMode == WhammyMode.SongBook)
                        {
                            if (i == 0)
                            {
                                var simpleStartX = cx + startNoteRenderer.X +
                                                   startNoteRenderer.GetBeatX(_beat, BeatXPosition.OnNotes)
                                                   - SimpleDipPadding * Scale;
                                var simpleEndX = cx + startNoteRenderer.X +
                                                 startNoteRenderer.GetBeatX(_beat, BeatXPosition.PostNotes)
                                                 + SimpleDipPadding * Scale;
                                var middleX = (simpleStartX + simpleEndX) / 2;
                                var text = ((_beat.WhammyBarPoints[1].Value - _beat.WhammyBarPoints[0].Value) / 4)
                                    .ToString();
                                canvas.Font = Renderer.Resources.TablatureFont;
                                canvas.FillText(text, middleX, cy + Y);

                                var simpleStartY = cy + Y + canvas.Font.Size + 2 * Scale;
                                var simpleEndY = simpleStartY + SimpleDipHeight * Scale;

                                if (_beat.WhammyBarPoints[1].Value > _beat.WhammyBarPoints[0].Value)
                                {
                                    canvas.MoveTo(simpleStartX, simpleEndY);
                                    canvas.LineTo(middleX, simpleStartY);
                                    canvas.LineTo(simpleEndX, simpleEndY);
                                }
                                else
                                {
                                    canvas.MoveTo(simpleStartX, simpleStartY);
                                    canvas.LineTo(middleX, simpleEndY);
                                    canvas.LineTo(simpleEndX, simpleStartY);
                                }
                                canvas.Stroke();
                            }
                        }
                        else
                        {
                            var middleX = (startX + endX) / 2;

                            _bendNoteHeads[0].X = middleX - _bendNoteHeads[0].NoteHeadOffset;
                            _bendNoteHeads[0].Y = cy + startNoteRenderer.Y;
                            _bendNoteHeads[0].Paint(0, 0, canvas);
                            var middleValue = GetBendNoteValue(note, beat.WhammyBarPoints[1]);
                            var middleY = _bendNoteHeads[0].GetNoteValueY(middleValue) + heightOffset;
                            DrawBendSlur(canvas, startX, startY, middleX, middleY, direction == BeamDirection.Down,
                                Scale);

                            _bendNoteHeads[1].X = endX - _bendNoteHeads[1].NoteHeadOffset;
                            _bendNoteHeads[1].Y = cy + startNoteRenderer.Y;
                            _bendNoteHeads[1].Paint(0, 0, canvas);
                            endValue = GetBendNoteValue(note, beat.WhammyBarPoints[beat.WhammyBarPoints.Count - 1]);
                            endY = _bendNoteHeads[1].GetNoteValueY(endValue) + heightOffset;
                            DrawBendSlur(canvas, middleX, middleY, endX, endY, direction == BeamDirection.Down, Scale);
                        }

                        break;
                    case WhammyType.PrediveDive:
                    case WhammyType.Predive:
                        var preX = cx + startNoteRenderer.X +
                                   startNoteRenderer.GetBeatX(note.Beat, BeatXPosition.PreNotes);
                        preX += ((ScoreBeatPreNotesGlyph)startNoteRenderer.GetBeatContainer(note.Beat).PreNotes)
                            .PrebendNoteHeadOffset;

                        var preY = cy + startNoteRenderer.Y +
                                   startNoteRenderer.GetScoreY(startNoteRenderer.AccidentalHelper.GetNoteLineForValue(note.DisplayValue - note.Beat.WhammyBarPoints[0].Value / 2))
                                   + heightOffset;

                        DrawBendSlur(canvas, preX, preY, startX, startY, direction == BeamDirection.Down, Scale);

                        if (_bendNoteHeads.Count > 0)
                        {
                            _bendNoteHeads[0].X = endX - _bendNoteHeads[0].NoteHeadOffset;
                            _bendNoteHeads[0].Y = cy + startNoteRenderer.Y;
                            _bendNoteHeads[0].Paint(0, 0, canvas);

                            endValue = GetBendNoteValue(note, beat.WhammyBarPoints[beat.WhammyBarPoints.Count - 1]);
                            endY = _bendNoteHeads[0].GetNoteValueY(endValue) + heightOffset;
                            DrawBendSlur(canvas, startX, startY, endX, endY, direction == BeamDirection.Down, Scale);
                        }

                        break;
                }
            }

            canvas.TextAlign = textalign;
        }

        private int GetBendNoteValue(Note note, BendPoint bendPoint)
        {
            // NOTE: bendpoints are in 1/4 tones, but the note values are in 1/2 notes. 
            return note.DisplayValueWithoutBend + bendPoint.Value / 2;
        }
    }
}