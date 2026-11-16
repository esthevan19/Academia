using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>API de consulta dos treinos exibidos no aplicativo.</summary>
    /// <remarks>Os treinos A, B, C e D são exemplos fixos neste controller; não são consultados do banco de dados.</remarks>
    public class WorkoutsController : ControllerBase
    {
        /// <summary>GET /api/Workouts: devolve treinos com exercícios, séries, repetições, descansos e grupos musculares.</summary>
        [HttpGet]
        public IActionResult GetWorkouts()
        {
            var workouts = new[]
            {
                new
                {
                    id = "treino-a",
                    letter = "A",
                    name = "Peito & Tríceps",
                    category = "Hipertrofia",
                    estimatedTimeMinutes = 50,
                    exercises = new[]
                    {
                        new { name = "Supino Reto com Barra", sets = 4, reps = "10 a 12", restSeconds = 60, muscle = "Peitoral" },
                        new { name = "Supino Inclinado com Halteres", sets = 3, reps = "12", restSeconds = 60, muscle = "Peitoral Superior" },
                        new { name = "Crossover na Polia Média", sets = 3, reps = "15", restSeconds = 45, muscle = "Peitoral" },
                        new { name = "Tríceps Corda no Cross", sets = 4, reps = "12", restSeconds = 45, muscle = "Tríceps" },
                        new { name = "Tríceps Francês Unilateral", sets = 3, reps = "12", restSeconds = 45, muscle = "Tríceps" }
                    }
                },
                new
                {
                    id = "treino-b",
                    letter = "B",
                    name = "Costas & Bíceps",
                    category = "Hipertrofia",
                    estimatedTimeMinutes = 50,
                    exercises = new[]
                    {
                        new { name = "Puxada Frontal Aberta", sets = 4, reps = "10 a 12", restSeconds = 60, muscle = "Dorsal" },
                        new { name = "Remada Curvada com Barra", sets = 4, reps = "10", restSeconds = 60, muscle = "Costas Geral" },
                        new { name = "Remada Baixa no Triângulo", sets = 3, reps = "12", restSeconds = 45, muscle = "Dorsal" },
                        new { name = "Rosca Direta com Barra W", sets = 4, reps = "10 a 12", restSeconds = 45, muscle = "Bíceps" },
                        new { name = "Rosca Martelo com Halteres", sets = 3, reps = "12", restSeconds = 45, muscle = "Braquial / Antebraço" }
                    }
                },
                new
                {
                    id = "treino-c",
                    letter = "C",
                    name = "Pernas & Ombros",
                    category = "Força & Resistência",
                    estimatedTimeMinutes = 55,
                    exercises = new[]
                    {
                        new { name = "Agachamento Livre", sets = 4, reps = "10", restSeconds = 90, muscle = "Quadríceps / Glúteos" },
                        new { name = "Leg Press 45°", sets = 4, reps = "12", restSeconds = 60, muscle = "Pernas Completo" },
                        new { name = "Cadeira Extensora", sets = 3, reps = "15", restSeconds = 45, muscle = "Quadríceps" },
                        new { name = "Mesa Flexora", sets = 3, reps = "12", restSeconds = 45, muscle = "Posterior de Coxa" },
                        new { name = "Desenvolvimento com Halteres", sets = 4, reps = "10", restSeconds = 60, muscle = "Ombros" },
                        new { name = "Elevação Lateral", sets = 4, reps = "12 a 15", restSeconds = 45, muscle = "Deltoide Lateral" }
                    }
                },
                new
                {
                    id = "treino-d",
                    letter = "D",
                    name = "Abdômen & Funcional",
                    category = "Condicionamento",
                    estimatedTimeMinutes = 40,
                    exercises = new[]
                    {
                        new { name = "Abdominal Supra no Solo", sets = 4, reps = "20", restSeconds = 30, muscle = "Abdômen" },
                        new { name = "Prancha Isométrica", sets = 3, reps = "45 segundos", restSeconds = 30, muscle = "Core" },
                        new { name = "Elevação de Pernas na Barra", sets = 3, reps = "15", restSeconds = 45, muscle = "Abdômen Infra" },
                        new { name = "Esteira / Corrida Moderada", sets = 1, reps = "20 minutos", restSeconds = 0, muscle = "Cardiovascular" }
                    }
                }
            };

            return Ok(new { success = true, workouts });
        }
    }
}
