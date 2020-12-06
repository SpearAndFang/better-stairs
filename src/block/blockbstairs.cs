using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

public class BlockBStairs : Block
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (byPlayer.Entity.Controls.Sneak) //sneak place only
        {
            Block targetBlock = world.BlockAccessor.GetBlock(blockSel.Position);
            string newPath = targetBlock.Code.Path;
            if (newPath.Contains("normal") && !(newPath.Contains("betterstonepathstairs")) )
            {
                //System.Diagnostics.Debug.WriteLine("rotate " + newPath);
                //rotate
                if (newPath.Contains("-up")) newPath = newPath.Replace("-up", "-sideways");
                else if (newPath.Contains("-sideways")) newPath = newPath.Replace("-sideways", "-down");
                else if (newPath.Contains("-down")) newPath = newPath.Replace("-down", "-up");
                if (newPath.Contains("-snow")) newPath = newPath.Replace("-snow", "-free");
                targetBlock = api.World.GetBlock(targetBlock.CodeWithPath(newPath));
                world.BlockAccessor.SetBlock(targetBlock.BlockId, blockSel.Position);
                return true;
            }
            //else System.Diagnostics.Debug.WriteLine("dont rotate " + newPath);
        }
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
        if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) return false;

        Block blockToPlace = this;
        string newPath = blockToPlace.Code.Path;

        string facing;
        BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
        double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
        double dz = byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
        double angle = Math.Atan2(dx, dz);
        angle += Math.PI;
        angle /= Math.PI / 4;
        int halfQuarter = Convert.ToInt32(angle);
        halfQuarter %= 8;

        if (halfQuarter == 1) facing = "northwest";
        else if (halfQuarter == 2) facing = "west";
        else if (halfQuarter == 3) facing = "southwest";
        else if (halfQuarter == 4) facing = "south";
        else if (halfQuarter == 5) facing = "southeast";
        else if (halfQuarter == 6) facing = "east";
        else if (halfQuarter == 7) facing = "northeast";
        else facing = "north"; //0

        string horVer = "-";
        if (blockSel.Face.IsVertical)
        { horVer += blockSel.Face.ToString(); }
        else
        { horVer += (blockSel.HitPosition.Y < 0.5 ? BlockFacing.UP : BlockFacing.DOWN).ToString(); }

        newPath = newPath.Replace("north", facing);
        newPath = newPath.Replace("-up", horVer);

        if (halfQuarter % 2 != 0) //corner
        {
            newPath = newPath.Replace("-normal", "-outside"); //outside by default

            //offset block fix (i.e. grass)
            string facebase = blockSel.Face.ToString();
            if (blockSel.DidOffset)
            {
                if (facebase == "up") targetPos.Y += 1;
                else if (facebase == "down") targetPos.Y -= 1;
                else if (facebase == "east") targetPos.X += 1;
                else if (facebase == "west") targetPos.X -= 1;
                else if (facebase == "north") targetPos.Z -= 1;
                else if (facebase == "south") targetPos.Z += 1;
            }

            //check nearest two neighbors
            BlockPos[] neibPos;
            Block testBlock;

            if (facing == "northwest") neibPos = new BlockPos[] { targetPos.NorthCopy(), targetPos.WestCopy() };
            else if (facing == "northeast") neibPos = new BlockPos[] { targetPos.NorthCopy(), targetPos.EastCopy() };
            else if (facing == "southwest") neibPos = new BlockPos[] { targetPos.SouthCopy(), targetPos.WestCopy() };
            else neibPos = new BlockPos[] { targetPos.SouthCopy(), targetPos.EastCopy() };
            int nCnt = 0; int sCnt = 0; int eCnt = 0; int wCnt = 0; int inCnt = 0; int outCnt = 0;
            foreach (BlockPos neib in neibPos)
            {
                testBlock = api.World.BlockAccessor.GetBlock(neib);
                if (testBlock.BlockId != 0)
                {
                    if (testBlock.Code.Path.Contains("stairs"))
                    {
                        if (testBlock.Code.Path.Contains("-north-")) nCnt++;
                        if (testBlock.Code.Path.Contains("-south-")) sCnt++;
                        if (testBlock.Code.Path.Contains("-east-")) eCnt++;
                        if (testBlock.Code.Path.Contains("-west-")) wCnt++;
                        if (testBlock.Code.Path.Contains("-inside-")) inCnt++;
                        if (testBlock.Code.Path.Contains("-outside-")) outCnt++;
                    }
                }
            }
            //System.Diagnostics.Debug.WriteLine("nsewio: " + nCnt + " " + sCnt + " " + eCnt + " " + wCnt + " " + inCnt + " " + outCnt);

            bool inOut = false;
            if (newPath.Contains("northwest") && (nCnt > sCnt || wCnt > eCnt)) inOut = true;
            else if (newPath.Contains("southwest") && (sCnt > nCnt || wCnt > eCnt)) inOut = true;
            else if (newPath.Contains("northeast") && (nCnt > sCnt || eCnt > wCnt)) inOut = true;
            else if (newPath.Contains("southeast") && (sCnt > nCnt || eCnt > wCnt)) inOut = true;
            else if (inCnt > outCnt) inOut = true;

            if (inOut) newPath = newPath.Replace("-outside", "-inside");
            else
            {
                //sideways?
                //check block below and then above, if an inside corner or sideways make this block sideways too

                neibPos = new BlockPos[] { targetPos.DownCopy(), targetPos.UpCopy() };
                int sideCnt = 0; inCnt = 0;
                foreach (BlockPos neib in neibPos)
                {
                    testBlock = api.World.BlockAccessor.GetBlock(neib);
                    if (testBlock.BlockId != 0)
                    {
                        if (testBlock.Code.Path.Contains("stairs"))
                        {
                            if (testBlock.Code.Path.Contains("-sideways-")) sideCnt++;
                            if (testBlock.Code.Path.Contains("-inside-")) inCnt++;
                        }
                    }
                }
                //System.Diagnostics.Debug.WriteLine("si: " + sideCnt + " " + inCnt);
                if (sideCnt > 0 || inCnt > 0)
                {
                    newPath = newPath.Replace(horVer, "-sideways");
                    newPath = newPath.Replace("-inside", "-normal");
                    newPath = newPath.Replace("-outside", "-normal");
                    if (halfQuarter == 3) newPath = newPath.Replace("southwest", "south");
                    else if (halfQuarter == 5) newPath = newPath.Replace("southeast", "east");
                    else if (halfQuarter == 7) newPath = newPath.Replace("northeast", "north");
                    else newPath = newPath.Replace("northwest", "west"); //1
                }
            }
        }
        if (newPath.Contains("betterstonepathstairs"))
        {
            newPath = newPath.Replace("-down", "-up");
            newPath = newPath.Replace("-sideways", "-up");
        }
        blockToPlace = api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
        world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
        return true;
    }
}