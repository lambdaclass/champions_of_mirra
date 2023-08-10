use rand::{Rng, rngs::ThreadRng};
use rustler::{NifMap, NifTaggedEnum};

use crate::player::Position;

#[derive(NifMap)]
pub struct Loot {
    pub id: u64,
    pub loot_type: LootType,
    pub position: Position,
}

#[derive(NifTaggedEnum)]
pub enum LootType {
    Health(u64),
}

pub fn spawn_random_loot(id: u64, max_x: usize, max_y: usize) -> Loot {
    let rng = &mut rand::thread_rng();
    let position = Position { x: rng.gen_range(0..max_x), y: rng.gen_range(0..max_y) };
    match rng.gen_range(0..1) {
        _0 => random_health_loot(id, position, rng),
    }
}

fn random_health_loot(id: u64, position: Position, rng: &mut ThreadRng) -> Loot {
    let value:u64 = rng.gen_range(25..75);
    let loot_type = LootType::Health(value);
    Loot { id, position, loot_type}
}
