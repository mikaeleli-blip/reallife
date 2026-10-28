using RealLife.Simulation;

namespace RealLife.Game.Art
{
    public static partial class BuildingArt
    {
        /// <summary>The models of the kinds of business added later.</summary>
        static void Extra(MeshKit k, BuildingType t, float w, float d, ref uint s, bool closed, int season, int floors)
        {
            switch (t)
            {
                // farms
                case BuildingType.DairyFarm: DairyFarm(k, w, d, ref s, closed, season); break;
                case BuildingType.CattleRanch: CattleRanch(k, w, d, ref s, closed, season); break;
                case BuildingType.PoultryFarm: PoultryFarm(k, w, d, ref s, closed, season); break;
                case BuildingType.TreeFarm: TreeFarm(k, w, d, ref s, closed, season); break;
                case BuildingType.Orchard: Orchard(k, w, d, ref s, closed, season); break;
                case BuildingType.Vineyard: Vineyard(k, w, d, ref s, closed, season); break;
                case BuildingType.PlantNursery: PlantNursery(k, w, d, ref s, closed, season); break;
                // mills and plants
                case BuildingType.Sawmill: Sawmill(k, w, d, ref s, closed, season); break;
                case BuildingType.PaperMill: PaperMill(k, w, d, ref s, closed, season); break;
                case BuildingType.SteelMill: SteelMill(k, w, d, ref s, closed, season); break;
                case BuildingType.MetalShop: MetalShop(k, w, d, ref s, closed, season); break;
                case BuildingType.AutoPartsPlant: AutoPartsPlant(k, w, d, ref s, closed, season); break;
                case BuildingType.ChemicalPlant: ChemicalPlant(k, w, d, ref s, closed, season); break;
                case BuildingType.PlasticsPlant: PlasticsPlant(k, w, d, ref s, closed, season); break;
                case BuildingType.TextileMill: TextileMill(k, w, d, ref s, closed, season); break;
                case BuildingType.Tannery: Tannery(k, w, d, ref s, closed, season); break;
                case BuildingType.Glassworks: Glassworks(k, w, d, ref s, closed, season); break;
                case BuildingType.Quarry: Quarry(k, w, d, ref s, closed, season); break;
                case BuildingType.Brickworks: Brickworks(k, w, d, ref s, closed, season); break;
                case BuildingType.ConcretePlant: ConcretePlant(k, w, d, ref s, closed, season); break;
                case BuildingType.FlourMill: FlourMill(k, w, d, ref s, closed, season); break;
                case BuildingType.BreadFactory: BreadFactory(k, w, d, ref s, closed, season); break;
                case BuildingType.DairyPlant: DairyPlant(k, w, d, ref s, closed, season); break;
                case BuildingType.Meatpacking: Meatpacking(k, w, d, ref s, closed, season); break;
                case BuildingType.Cannery: Cannery(k, w, d, ref s, closed, season); break;
                case BuildingType.Brewery: Brewery(k, w, d, ref s, closed, season); break;
                case BuildingType.Winery: Winery(k, w, d, ref s, closed, season); break;
                case BuildingType.Distillery: Distillery(k, w, d, ref s, closed, season); break;
                case BuildingType.BottlingPlant: BottlingPlant(k, w, d, ref s, closed, season); break;
                case BuildingType.PetFoodPlant: PetFoodPlant(k, w, d, ref s, closed, season); break;
                // factories and workshops
                case BuildingType.ShoeFactory: ShoeFactory(k, w, d, ref s, closed, season); break;
                case BuildingType.AppliancePlant: AppliancePlant(k, w, d, ref s, closed, season); break;
                case BuildingType.ToolWorks: ToolWorks(k, w, d, ref s, closed, season); break;
                case BuildingType.PaintFactory: PaintFactory(k, w, d, ref s, closed, season); break;
                case BuildingType.PharmaLab: PharmaLab(k, w, d, ref s, closed, season); break;
                case BuildingType.SoapWorks: SoapWorks(k, w, d, ref s, closed, season); break;
                case BuildingType.PrintingPress: PrintingPress(k, w, d, ref s, closed, season); break;
                case BuildingType.PaperGoodsPlant: PaperGoodsPlant(k, w, d, ref s, closed, season); break;
                case BuildingType.ToyFactory: ToyFactory(k, w, d, ref s, closed, season); break;
                case BuildingType.SportsGoodsFactory: SportsGoodsFactory(k, w, d, ref s, closed, season); break;
                case BuildingType.BicycleWorks: BicycleWorks(k, w, d, ref s, closed, season); break;
                case BuildingType.InstrumentWorkshop: InstrumentWorkshop(k, w, d, ref s, closed, season); break;
                case BuildingType.JewelryWorkshop: JewelryWorkshop(k, w, d, ref s, closed, season); break;
                case BuildingType.Pottery: Pottery(k, w, d, ref s, closed, season); break;
                case BuildingType.MattressFactory: MattressFactory(k, w, d, ref s, closed, season); break;
                case BuildingType.CabinetShop: CabinetShop(k, w, d, ref s, closed, season); break;
                default: ExtraShops(k, t, w, d, ref s, closed, season, floors); break;
            }
        }
    }
}
