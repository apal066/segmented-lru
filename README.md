To understand cache eviction techniques and segmented LRU mechanism visit my [blog](https://techanalogy.blog/cache-eviction-policies/).

# segmented-lru

Segmented LRU is not natively supported by Redis. This my implementation of segmented LRU using .NET and Redis.

## Prerequisites

1. Docker
2. Visual Studio / VS Code

## How to run?

1. Navigate to segmented-lru\SegmentedLRU
2. `docker-compose up --build`
3. browse http://localhost:5000/swagger

## Endpoints

![APIs](https://github.com/user-attachments/assets/d26aed77-02e5-4073-bca5-112d86460026)

## Assumptions

1. hotCacheCapacity = 2;  
2. coldCacheCapacity = 3;
3. promotionThreshold = 1;

## Simulate segmented LRU

### Step 1: Create 3 users using POST. Current cache status - everything in cold cache

```json

{
  "coldCacheKeys": [
    {
      "key": "1",
      "lastAccessTime": "2024-09-11T09:48:31.8264802+00:00",
      "frequency": 1
    },
    {
      "key": "2",
      "lastAccessTime": "2024-09-11T12:53:05.1473897+00:00",
      "frequency": 1
    },
    {
      "key": "3",
      "lastAccessTime": "2024-09-11T12:53:30.7641133+00:00",
      "frequency": 1
    }
  ],
  "hotCacheKeys": []
}
```

### Step 2: Access 1 & 3 using GET API. Both are promoted to hot cache

```json
{
  "coldCacheKeys": [
    {
      "key": "2",
      "lastAccessTime": "2024-09-11T12:53:05.1473897+00:00",
      "frequency": 1
    }
  ],
  "hotCacheKeys": [
    {
      "key": "1",
      "lastAccessTime": "2024-09-11T12:56:22.4475037+00:00",
      "frequency": 2
    },
    {
      "key": "3",
      "lastAccessTime": "2024-09-11T12:56:27.1889039+00:00",
      "frequency": 2
    }
  ]
}
```

### Step 3

Create 2 more users. Both hot and cold cache are full.

```json
{
  "coldCacheKeys": [
    {
      "key": "2",
      "lastAccessTime": "2024-09-11T12:53:05.1473897+00:00",
      "frequency": 1
    },
    {
      "key": "4",
      "lastAccessTime": "2024-09-11T12:58:57.1733056+00:00",
      "frequency": 1
    },
    {
      "key": "5",
      "lastAccessTime": "2024-09-11T12:59:05.0794484+00:00",
      "frequency": 1
    }
  ],
  "hotCacheKeys": [
    {
      "key": "1",
      "lastAccessTime": "2024-09-11T12:56:22.4475037+00:00",
      "frequency": 2
    },
    {
      "key": "3",
      "lastAccessTime": "2024-09-11T12:56:27.1889039+00:00",
      "frequency": 2
    }
  ]
}
```

### Step 4

Access 2 again. 2 is promoted to hot cache. 1 is demoted to cold cache.
```json
{
  "coldCacheKeys": [
    {
      "key": "1",
      "lastAccessTime": "2024-09-11T12:56:22.4475037+00:00",
      "frequency": 2
    },
    {
      "key": "4",
      "lastAccessTime": "2024-09-11T12:58:57.1733056+00:00",
      "frequency": 1
    },
    {
      "key": "5",
      "lastAccessTime": "2024-09-11T12:59:05.0794484+00:00",
      "frequency": 1
    }
  ],
  "hotCacheKeys": [
    {
      "key": "3",
      "lastAccessTime": "2024-09-11T12:56:27.1889039+00:00",
      "frequency": 2
    },
    {
      "key": "2",
      "lastAccessTime": "2024-09-11T13:01:03.9908779+00:00",
      "frequency": 2
    }
  ]
}
```

### Step 5

Create new user 6, 1 evicted from cold cache.

```json
{
  "coldCacheKeys": [
    {
      "key": "4",
      "lastAccessTime": "2024-09-11T12:58:57.1733056+00:00",
      "frequency": 1
    },
    {
      "key": "5",
      "lastAccessTime": "2024-09-11T12:59:05.0794484+00:00",
      "frequency": 1
    },
    {
      "key": "6",
      "lastAccessTime": "2024-09-11T13:02:58.4214785+00:00",
      "frequency": 1
    }
  ],
  "hotCacheKeys": [
    {
      "key": "3",
      "lastAccessTime": "2024-09-11T12:56:27.1889039+00:00",
      "frequency": 2
    },
    {
      "key": "2",
      "lastAccessTime": "2024-09-11T13:01:03.9908779+00:00",
      "frequency": 2
    }
  ]
}
```

