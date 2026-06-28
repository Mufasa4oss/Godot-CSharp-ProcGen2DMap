// M6 phase 3: how two nations relate, which drives trade across the route between them:
//   Friendly = free trade (no import tariff)
//   Neutral  = trade, but the importer levies a tariff (a barrier that suppresses imports)
//   Hostile  = blockade — no trade crosses at all
// Symmetric (a <-> b) and defaults to Neutral. The player sets their stance toward other nations as a
// strategic lever (ally for cheap goods, blockade to deny a rival export income).
public enum DiplomaticStance
{
    Hostile,
    Neutral,
    Friendly
}
