use lru::LruCache;
use std::num::NonZeroUsize;
use std::sync::Mutex;

pub struct ImageCache {
    cache: Mutex<LruCache<String, Vec<u8>>>,
}

impl ImageCache {
    pub fn new(capacity: usize) -> Self {
        Self {
            cache: Mutex::new(LruCache::new(
                NonZeroUsize::new(capacity).unwrap()
            )),
        }
    }

    pub fn get(&self, path: &str) -> Option<Vec<u8>> {
        self.cache.lock().unwrap().get(path).cloned()
    }

    pub fn put(&self, path: String, data: Vec<u8>) {
        self.cache.lock().unwrap().put(path, data);
    }

    pub fn clear(&self) {
        self.cache.lock().unwrap().clear();
    }
}
