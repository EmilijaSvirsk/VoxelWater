using System.Collections.Generic;

namespace VoxelWater
{
    static public class CellUtility
    {
        static public CellInfo EmptyCell = new CellInfo() { State = CellState.None };

        static public CellInfo SetState(CellInfo cellinfo, bool[] colliders)
        {
            if (cellinfo.State == CellState.Create || cellinfo.State == CellState.Remove)
                return cellinfo;

            cellinfo.OldState = cellinfo.State;

            int[] sides = getSideColliders(cellinfo, colliders);
            int sum = sides[0] + sides[1] + sides[2] + sides[3] + sides[4];

            bool down = ((cellinfo.BottomState == CellState.None || cellinfo.BottomState == CellState.Empty) && 
                        !ColliderExists(cellinfo, colliders, 0, -1, 0));

            //Cell emptyNeighbour = GetEmptyNeighbour(ref cellinfo, front, right, back, left, bottom);
            //bool surroundedByEmpty = SurroundedByEmpty(cellinfo);

            //if (surroundedByEmpty && cellinfo.Volume == 0)
            //    cellinfo.State = CellState.Destroy;
            if (cellinfo.Volume == 0)
                cellinfo.State = CellState.Empty;
            else if (down)
                cellinfo.State = CellState.Fall;
            else if (sum > 0 && cellinfo.Volume > 1)
                cellinfo.State = CellState.Flow;
            else if (cellinfo.BottomState != CellState.None &&
                (cellinfo.BottomState == CellState.Shallow))
                cellinfo.State = CellState.Merge;
            else if (sum == 0 && cellinfo.Volume == 1)
                cellinfo.State = CellState.Still;
            else if (sum == 0 && cellinfo.Volume > 1)
                cellinfo.State = CellState.Pressured;
            else if (sum > 0 && cellinfo.Volume == 1)
                cellinfo.State = CellState.Shallow;

            return cellinfo;
        }

        public static CellInfo ActivateStateInfo(CellInfo cellinfo, List<CellInfo> newCells, bool[] colliders)
        {
            switch (cellinfo.State)
            {
                case CellState.Flow:
                    newCells.AddRange(Flow(ref cellinfo, colliders, cellinfo.Volume));
                    break;
                case CellState.Pressured:
                    Pressured(ref cellinfo);
                    break;
                case CellState.Shallow:
                    Shallow(ref cellinfo);
                    break;
                case CellState.Fall:
                    CellInfo newCell = Fall(ref cellinfo);
                    if(newCell.State == CellState.None)
                    {
                        break;
                    }                 
                    else
                    {
                        newCells.Add(newCell);
                        break;
                    }
                case CellState.Destroy:
                    Destroy();
                    break;
                case CellState.Merge:
                    Merge(ref cellinfo);
                    break;
                //case CellState.Remove:
                //Remove();
                //break;
                case CellState.Create:
                    newCells.AddRange(Create(ref cellinfo, 5, colliders));
                    break;
                case CellState.Empty:
                    Destroy();
                    break;
            }
            return cellinfo;
        }

        static private List<CellInfo> Create(ref CellInfo cellinfo, int volume, bool[] colliders)
        {
            int[] sides = getSideColliders(cellinfo, colliders);
            int sum = sides[0] + sides[1] + sides[2] + sides[3] + sides[4];
            if (sum == 0)
            {
                FlowAll(ref cellinfo, volume);
                return new List<CellInfo>();
            }
            else
            {
                return Flow(ref cellinfo, colliders, volume);
            }    
        }

        static private List<CellInfo> Flow(ref CellInfo cellinfo, bool[] colliders, int cellVolume, bool decreaseVolume = true)
        {
            List<CellInfo> newCells = new List<CellInfo>();

            //flow to sides
            //(front, right, back, left)
            int[] sides = getSideColliders(cellinfo, colliders); //five array

            int sum = sides[0] + sides[1] + sides[2] + sides[3] + sides[4];
            int volumeEach = 0;
            int oldresidue = 0;
            int residue = 0;
            if (sum != 0)
            {
                volumeEach = (cellVolume - 1) / sum;
                residue = (cellVolume - 1) % sum;
                oldresidue = residue;
            }

            //front
            if (sides[0] == 1)
            {
                int volume = volumeEach;
                if (residue != 0)
                {
                    --residue;
                    volume += 1;
                }
                if (volume > 0)
                {
                    if (cellinfo.FrontState != CellState.None)
                        cellinfo.FrontVolume += volume;     
                    else
                        newCells.Add(CreateNewCellBase((int)cellinfo.X + 1, (int)cellinfo.Y + 0, (int)cellinfo.Z + 0, volume, CellState.Fall));
                }
            }
            //right
            if (sides[1] == 1)
            {
                int volume = volumeEach;
                if (residue != 0)
                {
                    --residue;
                    volume += 1;
                }
                if (volume > 0)
                {
                    if (cellinfo.RightState != CellState.None)
                        cellinfo.RightVolume += volume;
                    else
                        newCells.Add(CreateNewCellBase((int)cellinfo.X + 0, (int)cellinfo.Y + 0, (int)cellinfo.Z - 1, volume, CellState.Fall));
                }
            }
            //back
            if (sides[2] == 1)
            {
                int volume = volumeEach;
                if (residue != 0)
                {
                    --residue;
                    volume += 1;
                }
                if (volume > 0)
                {
                    if (cellinfo.BackState != CellState.None)
                        cellinfo.BackVolume += volume;
                    else
                        newCells.Add(CreateNewCellBase((int)cellinfo.X - 1, (int)cellinfo.Y + 0, (int)cellinfo.Z + 0, volume, CellState.Fall));
                }
            }
            //left
            if (sides[3] == 1)
            {
                int volume = volumeEach;
                if (residue != 0)
                {
                    --residue;
                    volume += 1;
                }
                if (volume > 0)
                {
                    if (cellinfo.LeftState != CellState.None)
                        cellinfo.LeftVolume += volume;
                    else
                        newCells.Add(CreateNewCellBase((int)cellinfo.X + 0, (int)cellinfo.Y + 0, (int)cellinfo.Z + 1, volume, CellState.Fall));
                }
            }

            //bottom
            if (sides[4] == 1)
            {
                int volume = volumeEach;
                if (residue != 0)
                {
                    --residue;
                    volume += 1;
                }
                if (volume > 0)
                {
                    if (cellinfo.BottomState != CellState.None)
                        cellinfo.BottomVolume += volume;
                    else
                        newCells.Add(CreateNewCellBase((int)cellinfo.X + 0, (int)cellinfo.Y - 1, (int)cellinfo.Z + 0, volume, CellState.Fall));
                }
            }
            if (decreaseVolume)
                cellinfo.Volume = cellinfo.Volume - (sum * volumeEach + oldresidue);

            return newCells;
        }

        static private CellInfo CreateNewCellBase(int x, int y, int z, int volume, CellState state)
        {
            return new CellInfo()
            {
                X = x,
                Y = y,
                Z = z,
                Volume = volume,
                State = state
            };
        }

        static private void FlowAll(ref CellInfo cellinfo, int volume)
        {
            if (cellinfo.BottomState != CellState.None)
                cellinfo.BottomVolume += volume;
            if (cellinfo.FrontState != CellState.None)
                cellinfo.FrontVolume += volume;
            if (cellinfo.RightState != CellState.None)
                cellinfo.RightVolume += volume;
            if (cellinfo.BackState != CellState.None)
                cellinfo.BackVolume += volume;
            if (cellinfo.LeftState != CellState.None)
                cellinfo.LeftVolume += volume;
        }

        static private int[] getSideColliders(CellInfo cellinfo, bool[] colliders)
        {
            int[] sides = { 0, 0, 0, 0, 0 };
            //front
            if ((cellinfo.FrontState == CellState.None || cellinfo.FrontState == CellState.Empty) && !ColliderExists(cellinfo, colliders, 1, 0, 0))
                sides[0] = 1;
            //right
            if ((cellinfo.RightState == CellState.None || cellinfo.RightState == CellState.Empty) && !ColliderExists(cellinfo, colliders, 0, 0, -1))
                sides[1] = 1;
            //back
            if ((cellinfo.BackState == CellState.None || cellinfo.BackState == CellState.Empty) && !ColliderExists(cellinfo, colliders, -1, 0, 0))
                sides[2] = 1;
            //left
            if ((cellinfo.LeftState == CellState.None || cellinfo.LeftState == CellState.Empty) && !ColliderExists(cellinfo, colliders, 0, 0, 1))
                sides[3] = 1;
            //bottom
            if ((cellinfo.BottomState == CellState.None || cellinfo.BottomState == CellState.Empty) && !ColliderExists(cellinfo, colliders, 0, -1, 0))
                sides[4] = 1;

            return sides;
        }

        static private bool ColliderExists(CellInfo cellinfo, bool[] colliders, float x, float y, float z)
        {
            //convert coordinates for colliders array
            int xcoll = cellinfo.Xgrid + 1 + (int)x;
            int ycoll = cellinfo.Ygrid + 1 + (int)y;
            int zcoll = cellinfo.Zgrid + 1 + (int)z;
            return CellInfoUtility.Get(xcoll, ycoll, zcoll, cellinfo.GridSizeCI, colliders);
        }
        static private void Pressured(ref CellInfo cellinfo)
        {
            GiveVolume(ref cellinfo, 1);
        }

        static private void GiveVolume(ref CellInfo cellinfo, int volume)
        {
            //grid.GiveVolume(volume);
            cellinfo.Volume -= volume;
        }

        static private void Shallow(ref CellInfo cellinfo)
        {
            GetVolume(ref cellinfo,1);
        }

        static private void GetVolume(ref CellInfo cellinfo, int volume)
        {
            //works only with volume 1
            //if (grid.GetVolume(volume))
            //{
            cellinfo.Volume += volume;
            //}
        }

        static CellInfo Fall(ref CellInfo cellinfo)
        {
            if (cellinfo.BottomState == CellState.None)
            {
                CellInfo newCell = CreateNewCellBase((int)cellinfo.X + 0, (int)cellinfo.Y - 1, (int)cellinfo.Z + 0, cellinfo.Volume, CellState.Fall);
                cellinfo.Volume = 0;
                return newCell;
            }   
            else
            {
                cellinfo.BottomVolume += cellinfo.Volume;
                cellinfo.Volume = 0;
                return EmptyCell;
            }        
        }

        static private void Merge(ref CellInfo cellinfo)
        {
            cellinfo.BottomVolume += cellinfo.Volume;
            cellinfo.Volume = 0;
        }

        static private void Destroy()
        {
            //grid.VolumeExcess += cellinfo.Volume;
            //DeleteCell();
        }

        /*
        static private bool SurroundedByEmpty(CellInfo cellinfo)
        {
            if ((cellinfo.BottomState != CellState.None && cellinfo.BottomState != CellState.Empty))
                return false;
            if ((cellinfo.FrontState != CellState.None && cellinfo.FrontState != CellState.Empty))
                return false;
            if ((cellinfo.RightState != CellState.None && cellinfo.RightState != CellState.Empty))
                return false;
            if ((cellinfo.BackState != CellState.None && cellinfo.BackState != CellState.Empty))
                return false;
            if ((cellinfo.LeftState != CellState.None && cellinfo.LeftState != CellState.Empty))
                return false;
            if ((cellinfo.TopState != CellState.None && cellinfo.TopState != CellState.Empty))
                return false;
            //if ((bottom != null && bottom.State != CellState.Empty))
            //    return false;
            //if ((top != null && top.State != CellState.Empty))
            //    return false;

            return true;
        }
        */
    }
}
