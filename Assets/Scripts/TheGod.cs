using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityRandom = UnityEngine.Random;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class TheGod : MonoBehaviour
{
    [SerializeField] private List<Trap> _traps;
    [Space(10)]
    [SerializeField] private List<Animal> _animals;
    [SerializeField] private int initAnimalsCount = 5;
    [SerializeField] private SpriteRenderer _spawnObjectsPrefab;
    [Space(10)]
    [SerializeField] private List<Zona> _zones;
    [Space(10)]
    [SerializeField] private ValueDisplayer _moneyDisplayer;
    [Space(10)]
    [SerializeField] private GameTimers _gameTimers;
    [Space(10)]
    [SerializeField] private TrapsInventory _trapsInventory;
    [Space(10)]
    [SerializeField] private SettingUI _settingUI;
    [Space(10)]
    [SerializeField] private GameOverUI _gameOverUI;
    [Space(10)]
    [SerializeField] private AudioMixer _audioMixer;
    [Space(10)]
    [SerializeField] private int _startMoney = 100;
    [SerializeField] private int _taxesMin = 15;
    [SerializeField] private int _taxesMax = 35;


    private ChangingValue _money = new ChangingValue();

    private bool _isPaused = false;
    private bool _isMusicPlayed = true;



    private void Awake()
    {
        InitMoney();

        _trapsInventory.Init(_traps);
        InitZones();
        InitTrapUI();
        InitSettingUI();
        _gameOverUI.Init();
    }
    
    private void InitSettingUI()
    {
        _settingUI.Init();
        _settingUI.SoundButtonClick += () =>
        {
            if (_isMusicPlayed)
            {
                _audioMixer.SetFloat("Volume", -80);
            }
            else
            {
                _audioMixer.SetFloat("Volume", 10);
            }
            _isMusicPlayed = !_isMusicPlayed;
        };
        _settingUI.PauseButtonClick += () =>
         {
             if (_isPaused)
             {
                 Time.timeScale = 1;
             }
             else
             {
                 Time.timeScale = 0;
             }
             _isPaused = !_isPaused;
         };
    }

    private void Start()
    {
        StartCoroutine(SpawnAnimalsCorun());
        StartCoroutine(HuntAnimalsCorun());
        StartCoroutine(TaxesCorun());
    }


    private void InitMoney()
    {
        _moneyDisplayer.Init(_money);
        _money.AddValue(_startMoney);
        _money.Changed += OnMoneyChanged;
    }
    private void InitZones()
    {
        foreach (var zone in _zones)
        {
            zone.AnimalKilled += x => { _settingUI.PauseButtonClick?.Invoke(); _gameOverUI.ShowGameOver($"Population {x.Name} was killed. You're fired"); };
            zone.AnimalOverpopulated += x => { _settingUI.PauseButtonClick?.Invoke(); _gameOverUI.ShowGameOver($"Population {x.Name} took over the world. Now you are a slave"); };

            zone.Init(_animals, _traps);
            zone.TrapAdded += OnTrappAdded;
            zone.Hunted +=(x,y) => { DestroyGO(x); DestroyGO(y); };
            foreach (var animal in _animals)
            {
                for (int i = 0; i < initAnimalsCount; i++)
                {
                    SpawnAnimal(animal);
                }
            }
        }
    }

    private void DestroyGO(GameObject obj)
    {
        Destroy(obj);
    }

    private void OnTrappAdded(Trap trap, Zona zone)
    {
        _trapsInventory.RemoveTrapp(trap);
        Vector2 randomPos;
        randomPos.x = UnityRandom.Range(_zones[0].Start.position.x, _zones[0].End.position.x);
        randomPos.y = UnityRandom.Range(_zones[0].Start.position.y, _zones[0].End.position.y);
        var trapSpriteRender = Instantiate(_spawnObjectsPrefab, randomPos, Quaternion.identity);
        trapSpriteRender.sprite = trap.Icon;

        zone.AddTrap(trapSpriteRender.gameObject, trap);
        if(_trapsInventory.Count(trap) == 0)
        {
            foreach(var z in _zones)
            {
                z.TurnOffAddButton(trap);
            }
        }
    }

    private void InitTrapUI()
    {
        foreach (var trap in _traps)
        {
            trap.Init();
            trap.Buyed += BuyTrap;
        }
    }
    private void OnMoneyChanged(int value)
    {
        if(value <= 0)
        {
            _settingUI.PauseButtonClick?.Invoke();
            _gameOverUI.ShowGameOver("You don't have money anymore so you die");
        }
        else
            if(value>=1000)
        {
            _settingUI.PauseButtonClick?.Invoke();
            _gameOverUI.ShowGameOver("You earned enough money for all life)))");

        }
        foreach (var trap in _traps)
        {
            if(value < trap.Cost)
            {
                trap.TurnOffBuyButton();
                continue;
            }
            trap.TurnOnBuyButton();
        }
    }

    private IEnumerator TaxesCorun()
    {
        WaitForSeconds waiter = new WaitForSeconds(_gameTimers.TaxesTime);
        while (true)
        {
            yield return waiter;
            _money.AddValue(-UnityRandom.Range(_taxesMin,_taxesMax));
        }
    }

    private IEnumerator HuntAnimalsCorun()
    {
        WaitForSeconds waiter = new WaitForSeconds(_gameTimers.HuntTime);

        while (true)
        {
            yield return waiter;
            foreach (var zone in _zones)
            {
                _money.AddValue(zone.HuntInZone());
            }
        }
    }

    private IEnumerator SpawnAnimalsCorun()
    {
        WaitForSeconds waiter = new WaitForSeconds(_gameTimers.AnimalSpawnTime);
        while (true)
        {
            yield return waiter;

            foreach (var animal in _animals)
            {
                int changes = _zones[0].Count(animal);
                if (changes >= 10)
                    continue;
                if (changes >= UnityRandom.Range(0, 10))
                {
                    SpawnAnimal(animal);
                }

            }
        }
    }

    private void SpawnAnimal(Animal animal)
    {
        Vector2 randomPos;
        randomPos.x = UnityRandom.Range(_zones[0].Start.position.x, _zones[0].End.position.x);
        randomPos.y = UnityRandom.Range(_zones[0].Start.position.y, _zones[0].End.position.y);
        var animalSpriteRender = Instantiate(_spawnObjectsPrefab, randomPos, Quaternion.identity);
        animalSpriteRender.sprite = animal.Icon;
        _zones[0].AddAnimal(animalSpriteRender.gameObject, animal);
    }

    private void BuyTrap(Trap trap)
    {
        _money.AddValue(trap.Cost * -1);
        _trapsInventory.AddTrapp(trap);
        foreach(var zone in _zones)
        {
            zone.TurnOnAddButton(trap);
        }
    }
}

#region Classes

[Serializable]
class GameTimers
{
    [field: SerializeField] public float TaxesTime { get; private set; } = 10f;
    [field: SerializeField] public float HuntTime { get; private set; } = 5f;
    [field: SerializeField] public float AnimalSpawnTime { get; private set; } = 1f;
}

[Serializable]
class TrapsInventory
{
    [SerializeField] private List<ValueDisplayer> _trapsCountDisplayers;

    public int Count(Trap trap) => _inventory[trap].CurrentValue;

    private Dictionary<Trap, ChangingValue> _inventory;

    public void Init(List<Trap> traps)
    {
        _inventory = new Dictionary<Trap, ChangingValue>();
        for (int i = 0; i < traps.Count; i++)
        {
            ChangingValue trapCount = new ChangingValue();
            _trapsCountDisplayers[i].Init(trapCount);
            _inventory.Add(traps[i], trapCount);
        }
    }

    public void RemoveTrapp(Trap trap)
    {
        if (_inventory.ContainsKey(trap))
        {
            _inventory[trap].AddValue(-1);
            return;
        }
    }

    public void AddTrapp(Trap trap)
    {
        if (_inventory.ContainsKey(trap))
        {
            _inventory[trap].AddValue(1);
            return;
        }
    }
}

[Serializable]
class ValueDisplayer
{
    [SerializeField] private TextMeshProUGUI _text;

    public void Init(ChangingValue valueChanging)
    {
        valueChanging.Changed += ChangeText;
    }

    private void ChangeText(int newValue)
    {
        _text.SetText($"{newValue}");
    }
}

[Serializable]
public class Zona
{
    public event Action<Animal> AnimalKilled;
    public event Action<Animal> AnimalOverpopulated;

    public event Action<Trap, Zona> TrapAdded;
    public event Action<GameObject, GameObject> Hunted;

    [field: SerializeField] public Transform Start { get; private set; }
    [field: SerializeField] public Transform End { get; private set; }

    [SerializeField] private List<ValueDisplayer> _countTexts;

    [SerializeField] private List<ValueDisplayer> _trapCountTexts;

    [SerializeField] private List<Button> _addButons;

    public int Count(Animal animal) => _animalsCountTexts[animal].CurrentValue;

    private List<AnimalInZone> _animals = new List<AnimalInZone>();
    private List<TrapInZone> _trapInZones = new List<TrapInZone>();

    private Dictionary<Animal, ChangingValue> _animalsCountTexts;
    private Dictionary<Trap, ChangingValue> _trapCounts;
    private Dictionary<Trap, Button> _buttonTrap;

    public void Init(List<Animal> animals, List<Trap> traps)
    {
        _animalsCountTexts = new Dictionary<Animal, ChangingValue>();
        for(int  i = 0; i < animals.Count; i++)
        {
            ChangingValue animalCount = new ChangingValue();
            _countTexts[i].Init(animalCount);
            _animalsCountTexts.Add(animals[i], animalCount);
        }
        _trapCounts = new Dictionary<Trap, ChangingValue>();
        for(int i = 0; i < traps.Count; i++)
        {
            ChangingValue trapCount = new ChangingValue();
            _trapCountTexts[i].Init(trapCount);
            _trapCounts.Add(traps[i], trapCount);
        }

        _buttonTrap = new Dictionary<Trap, Button>();
        for(int  i = 0; i < _addButons.Count; i++)
        {
            int temp = i;
            _buttonTrap.Add(traps[temp], _addButons[temp]);
            _addButons[temp].onClick.AddListener(() => { TrapAdded?.Invoke(traps[temp], this); });
            TurnOffAddButton(traps[temp]);
        }

    }

    public int HuntInZone()
    {
        List<AnimalInZone> needRemove = new List<AnimalInZone>();
        int huntResult = 0;
        Shuffle(_animals);
        foreach (AnimalInZone animal in _animals)
        {
            foreach (KeyValuePair<Trap, ChangingValue> trapChanginValuPair in _trapCounts)
            {
                float randomNumber = UnityRandom.Range(0f, 1f);
                for (int i = 0; i < trapChanginValuPair.Value.CurrentValue; i++)
                {
                    if (trapChanginValuPair.Key.GetChangeToHunt(animal.Animal.Type) >= randomNumber)
                    {
                        huntResult += animal.Animal.Cost;
                        trapChanginValuPair.Value.AddValue(-1);
                        var currentTrapInZone = _trapInZones.Find(x => x.Trap == trapChanginValuPair.Key);

                        Hunted?.Invoke(currentTrapInZone.GO, animal.GO);
                        _trapInZones.Remove(currentTrapInZone);
                        needRemove.Add(animal);
                        
                        break;
                    }
                }
            }
        }
        foreach (var animal in needRemove)
        {
            RemoveAnimal(animal);
            if (Count(animal.Animal) == 0)
            {
                AnimalKilled?.Invoke(animal.Animal);
            }
        }
        
        return huntResult;
    }

    public static void Shuffle(List<AnimalInZone> arr)
    {

        for (int i = arr.Count - 1; i >= 1; i--)
        {
            int j = UnityRandom.Range(0, i + 1);

            AnimalInZone tmp = arr[j];
            arr[j] = arr[i];
            arr[i] = tmp;
        }
    }

    public void AddTrap(GameObject trapGO, Trap trap)
    {
        _trapCounts[trap].AddValue(1);
        _trapInZones.Add(new TrapInZone(trapGO, trap));
    }

    public void TurnOnAddButton(Trap trap)
    {
        _buttonTrap[trap].interactable = true;
    }

    public void TurnOffAddButton(Trap trap)
    {
        _buttonTrap[trap].interactable = false;
    }

    public void AddAnimal(GameObject animalGO, Animal animal)
    {
        _animals.Add(new AnimalInZone(animalGO, animal));
        _animalsCountTexts[animal].AddValue(1);
        if (Count(animal) == 20)
        {
            AnimalOverpopulated?.Invoke(animal);
        }
    }

    public AnimalInZone GetAnimalAt(int index)
    { 
        return _animals[index];
    }

    public void RemoveAnimal(AnimalInZone animal)
    {
        _animalsCountTexts[animal.Animal].AddValue(-1);
        _animals.Remove(animal);
    } 
}

[Serializable]
public class Trap
{
    public event Action<Trap> Buyed;

    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
    [field: SerializeField] public int Cost { get; private set; }

    [SerializeField] private Button _buyBytton;

    [SerializeField] private TextMeshProUGUI _costText;

    [SerializeField] private List<ChangeToHunt> _changesToHunt;

    public void Init()
    {
        _costText.SetText($"{Cost}$");

        _buyBytton.onClick.AddListener(() => { Buyed?.Invoke(this); });
    }

    public float GetChangeToHunt(AnimalType animalType)
    {
        var needChangeToHunt = _changesToHunt.Find(x => x.AnimalType == animalType);

        if (needChangeToHunt != null)
        {
            return needChangeToHunt.Change;
        }
        return 0f;
    }

    public void TurnOnBuyButton()
    {
        _buyBytton.interactable = true;
    }

    public void TurnOffBuyButton()
    {
        _buyBytton.interactable = false;
    }
}

[Serializable]
public class Animal
{
    [field: SerializeField] public AnimalType Type { get; private set; }
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
    [field: SerializeField] public int Cost { get; private set; }
}

[Serializable]
class SettingUI
{
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _soundButton;

    public Action PauseButtonClick;
    public Action SoundButtonClick;

    public void Init()
    {
        _pauseButton.onClick.AddListener(() => { PauseButtonClick?.Invoke(); });
        _soundButton.onClick.AddListener(() => { SoundButtonClick?.Invoke(); });
    }
}

[Serializable]
class ChangingValue
{
    public event Action<int> Changed;
    public int CurrentValue { get; private set; }
    public void AddValue(int count)
    {
        CurrentValue += count;
        Changed?.Invoke(CurrentValue);
    }
}

[Serializable]
public class TrapInZone
{
    [field: SerializeField] public GameObject GO { get; private set; }
    [field: SerializeField] public Trap Trap { get; private set; }

    public TrapInZone(GameObject go, Trap trap)
    {
        GO = go;
        Trap = trap;
    }
}

[Serializable]
public class AnimalInZone
{
    [field: SerializeField] public GameObject GO { get; private set; }
    [field: SerializeField] public Animal Animal { get; private set; }

    public AnimalInZone(GameObject go, Animal animal)
    {
        GO = go;
        Animal = animal;
    }
}

[Serializable]
public class GameOverUI
{
    [SerializeField] private GameObject _parent;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Button _retryButton;

    public void Init()
    {
        _retryButton.onClick.AddListener(() => {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0); });
    }

    public void ShowGameOver(string whatHappened)
    {
        _parent.SetActive(true);
        _text.SetText(whatHappened);
    }
}

[Serializable]
public class ChangeToHunt
{
    [field: SerializeField] public AnimalType AnimalType { get; private set; }
    [field: SerializeField] public float Change { get; private set; }
}

#endregion

#region Enum
public enum AnimalType
{
    Little,
    Big,
    Bird
}
#endregion