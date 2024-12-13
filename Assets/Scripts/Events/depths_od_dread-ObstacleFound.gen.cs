using Dojo;
using Dojo.Starknet;

// Model definition for `depths_of_dread::systems::actions::actions::ObstacleFound` model
public class depths_of_dread_ObstacleFound : ModelInstance
{
    [ModelField("player")]
    public FieldElement player;

    [ModelField("obstacle_type")]
    public ObstacleType obstacle_type;

    [ModelField("obstacle_position")]
    public Vec2 obstacle_position;

    [ModelField("defended")]
    public bool defended;

    void Start()
    {
    }

    void Update()
    {
    }
}
