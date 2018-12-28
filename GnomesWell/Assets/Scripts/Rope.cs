using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Верёвка состоящая из звеньев
public class Rope : MonoBehaviour
{
    // Шаблон Rope Segment состоящая из новых звеньев.
    public GameObject ropeSegmentPrefab;

    // Список объектов Rope Segment.
    List<GameObject> ropeSegments = new List<GameObject>();

    // Верёвка удлиняется или укорачивается?
    public bool isIncreasing { get; set; }
    public bool isDecreasing { get; set; }

    // Объект твёрдого тела, к которому следует присоеденить конец верёвки.
    public Rigidbody2D connectedObject;

    // Максимальная длина звена верёвки
    // (если потребуется удлинить верёвку больше, чем на эту величину, 
    // то будет создано новое звено.)
    public float maxRopeSegmentLength = 1.0f;

    // Как быстро должны создаваться новые звенья верёвки?
    public float ropeSpeed = 4.0f;

    // Визуализатор LineRenderer, отображающий верёвку.
    LineRenderer lineRenderer;



    // Когда объект Rope появляется в первый раз, вызывается его метод Start.
    void Start()
    {
        // Кэшировать ссылку на визуализатор, чтобы не пришлось искать его в каждом кадре.
        lineRenderer = GetComponent<LineRenderer>();

        // Сбросить состояние верёвки в исходное.
        ResetLength();
    }

    // Удалить все звенья и создать новое.
    public void ResetLength()
    {
        foreach(GameObject segment in ropeSegments)
        {
            Destroy(segment);
        }

        ropeSegments = new List<GameObject>();

        isDecreasing = false;
        isIncreasing = false;

        CreateRopeSegment();
    }

    // Добовляет новое звено верёвки к верхнему концу.
    void CreateRopeSegment()
    {
        // Создать новое звено.
        GameObject segmment = (GameObject)Instantiate(
            ropeSegmentPrefab,
            this.transform.position,
            Quaternion.identity);

        // Сделать звено потомком объект this
        // и сохранить его мировые координаты
        segmment.transform.SetParent(this.transform, true);

        // Получить твёрдое тело звена
        Rigidbody2D segmentBody = segmment.GetComponent<Rigidbody2D>();

        // Получить длину сочленения из звена
        SpringJoint2D segmentJoint = segmment.GetComponent<SpringJoint2D>();

        // Ошибка, если если шаблон звена не имеет твёрдого тела или пружинного сочленения - 
        // нужны оба.
        if (segmentBody == null || segmentJoint == null)
        {
            Debug.LogError("Rope segment body prefab has no Rigi");
            return;
        }

        // Теперь, после всех проверок, можно добавить новое звено в начало списка звеньев.
        ropeSegments.Insert(0, segmment);

        // Если это *первое* звено, его нужно соединить с ногой гномика
        if (ropeSegments.Count == 1)
        {
            // Соединить звено с сочленением несущего объекта
            SpringJoint2D connectedObjectJoint = connectedObject.GetComponent<SpringJoint2D>();

            connectedObjectJoint.connectedBody = segmentBody;

            connectedObjectJoint.distance = 0.1f;

            // Установить длину звена в максимальное значение
            segmentJoint.distance = maxRopeSegmentLength;
        }
        else
        {
            // Это не первое звеоню. Его нужно соеденить с предыдущим звеном.

            // Получить второе звено.
            GameObject nextSegment = ropeSegments[1];

            // получить сочленение для соединения.
            SpringJoint2D nextSegmentJoint = nextSegment.GetComponent<SpringJoint2D>();

            // Присоеденить сочленение к новому звену.
            nextSegmentJoint.connectedBody = segmentBody;

            // Установить начальную длину сочленения нового звена равной 0 - она увеличется автоматически.
            segmentJoint.distance = 0.0f;
        }

        // Создание нового звена с опорой для верёвки ( то есть с объектом this).
        segmentJoint.connectedBody = this.GetComponent<Rigidbody2D>();
    }

    // Вызывается, когда нужно укоротить верёвку и удалаяет звено сверху.
    void RemoveRopeSegment()
    {
        // Если звеньев меньше двух, выйти.
        if (ropeSegments.Count < 2)
        {
            return;
        }

        // Получить верхнее звено и звено под ним.
        GameObject topSegment = ropeSegments[0];
        GameObject nextSegment = ropeSegments[1];

        // Соединенить второе звено с опорюй для верёвки.
        SpringJoint2D nextSegmentJoin = nextSegment.GetComponent<SpringJoint2D>();

        nextSegmentJoin.connectedBody = this.GetComponent<Rigidbody2D>();

        // Удалить верхнее звено из списка и удалить его.
        ropeSegments.RemoveAt(0);
        Destroy(topSegment);
    }

    // При необходимости в каждом кадре длина верёвки
    // удлиняется или укорачивается.
    void Update()
    {
        // Получить верхнее звено и его сочленение.
        GameObject topSegment = ropeSegments[0];
        SpringJoint2D topSegmentJoin = topSegment.GetComponent<SpringJoint2D>();

        if (isIncreasing)
        {
            // Верёвку нужно удлинить. Если длина сочленения больше или равна максимальной,
            // добавляется новое звено;
            // иначе увеличевается длина сочленения звена.

            if (topSegmentJoin.distance >= maxRopeSegmentLength)
            {
                CreateRopeSegment();
            }
            else
            {
                topSegmentJoin.distance += ropeSpeed * Time.deltaTime;
            }
        }

        if (isDecreasing)
        {
            // Верёвку нужно удлинить. Если длина сочленения близка к нулю, 
            // удалить звено; иначе уменьшить длину сочленения верхнего звена.

            if (topSegmentJoin.distance <= 0.005f)
            {
                RemoveRopeSegment();
            }
            else
            {
                topSegmentJoin.distance -= ropeSpeed * Time.deltaTime;
            }
        }

        if(lineRenderer != null)
        {
            // Визуализатор LineRenderer рисует линию по коллекции точек.
            // Эти точки должны соответствовать позициям звеньев верёвки.

            // Число вершин, отображаемых визуализатором, равно числу звеньев
            // плюс одна точка на верхней опроре. Плюс одна точка на ноге гномика.
            lineRenderer.positionCount = ropeSegments.Count + 2;

            // Верхняя вершина всегда соответствует положению опроры.
            lineRenderer.SetPosition(0, this.transform.position);

            // Передать визуализатору координаты всех звеньев верёвки.
            for(int i=0; i < ropeSegments.Count; i++)
            {
               lineRenderer.SetPosition(i + 1, ropeSegments[i].transform.position);
            }

            // Последняя точка соответствует последней точке несущего объекта.
            SpringJoint2D connectedObjectJoint = connectedObject.GetComponent<SpringJoint2D>();
            lineRenderer.SetPosition(ropeSegments.Count + 1, connectedObject.transform.TransformPoint(connectedObjectJoint.anchor));
        }
    }
}
