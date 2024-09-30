# Notification Rate-Limiting System

This project implements a basic notification rate-limiting system using **AWS Lambda** or **API Gateway**. The system is functional and can be deployed as a serverless function, but it is not the state-of-the-art. This design can be improved in the future with features like prioritization of notifications or using on-demand workers to group and throttle notifications effectively.

## Key Components

### 1. Basic Rate Limiting

The **appsettings.json** file contains rate limits for different types of notifications. These limits are simple and configurable

The current design handles rate-limiting in a basic way, which is sufficient for simple use cases but may not scale well under heavy load or burst actions. Specifically, this implementation is vulnerable to the following challenges:

1. **Handling Burst Traffic**: The system may struggle with sudden surges of notifications. Although the rate limits are configured to control the number of requests, they are static and do not adjust dynamically to handle high volumes, potentially leading to delayed or dropped notifications.

2. **Equal Treatment of Notifications**: Currently, all notifications are treated the same. There is no distinction between critical and non-critical notifications. In a more advanced system, notifications could be prioritized based on their urgency or category (e.g., system alerts vs. marketing).

3. **Limited Flexibility in Throttling**: The rate limits defined in `appsettings.json` are static and apply globally across all requests. In future iterations, the system could incorporate dynamic rate-limiting, where limits adjust in real-time based on current server load or user behavior.

---

## API Endpoints

### Example API Request

You can test the notification API with the following cURL command. This sends a simple notification request and helps you verify the rate-limiting functionality:

```bash
curl -X POST "https://localhost:5000/notifications" \
-H "Content-Type: application/json" \
-d '{
  "message": "This is a test notification",
  "type": "Status"
}'
```

This cURL command sends a test notification of the type `Status`. Depending on the rate limits for the `Status` type, the response will vary based on whether the rate limit has been exceeded.

If the rate limit has not been exceeded, the API will return a 200 OK response, along with the following headers:

- **X-RateLimit-Limit**: The total number of allowed requests in the rate limit window.
- **X-RateLimit-Remaining**: The number of requests remaining within the current window.
- **X-RateLimit-Reset**: The time, in seconds, until the rate limit resets and you can send new requests.
![image](https://github.com/user-attachments/assets/5397fb47-45e3-4a17-b70d-f58c97ae7472)

If the rate limit is exceeded, the API will return a 429 Too Many Requests response, along with the same headers, indicating the rate limit details and when it will reset.

### Grafana Monitoring and Visualization 

the system integrates with **Prometheus** and **Grafana** for monitoring the notification traffic and rate-limiting behavior. You can visualize the performance of the API, including the rate of successful requests (HTTP 200) and the frequency of rate-limited requests (HTTP 429), in the Grafana dashboard.
a simple scenario would be like: ![Screenshot 2024-09-30 182509](https://github.com/user-attachments/assets/9ee87cf3-6e19-4f9d-bd16-17e9e3bb1541)
if too many request starts to go above threadshold, there's something wrong with the rule, or someone's triggering too many times the api 


## Next Steps and Improvements

While this implementation provides a good starting point for handling notifications with rate-limiting, there are several enhancements that could be added to make the system more robust and scalable.

### 1. Dynamic Rate-Limiting

Currently, rate limits are static and configured manually. Dynamic rate-limiting would allow the system to adjust based on current load, traffic, or other external factors. For example:

- Increase rate limits during off-peak hours.
- Decrease rate limits when the server is under heavy load to prevent outages.

This could be achieved by integrating with a load-monitoring service and dynamically adjusting the rate limits via a control plane or API.

### 2. Notification Prioritization

In some use cases, certain notifications (e.g., system alerts or security warnings) may need to bypass or have higher priority over less critical notifications like marketing messages. Introducing a priority queue would ensure that important notifications are delivered even under heavy load.

- High-priority messages could be delivered without delay.
- Low-priority notifications could be throttled or delayed during peak traffic periods.

This could be implemented by assigning a priority level to each notification type and processing them based on their priority.

### 3. Retry Mechanisms

When the system returns a `429 Too Many Requests` response, it currently relies on the client to handle retries. However, the backend could be improved by implementing an automatic retry mechanism that queues rate-limited notifications and retries them when the rate limit resets.

This retry mechanism could be part of an **on-demand worker** architecture where failed notifications are queued, and workers retry them automatically after the reset time.

### 4. On-Demand Worker Architecture

As traffic grows, the system may need to scale up to handle large bursts of notifications. An **on-demand worker** architecture would:

- Queue notifications and process them in batches.
- Automatically scale up and down workers based on demand.
- Throttle notifications to avoid exceeding rate limits while maintaining performance.

This architecture would improve the system's scalability and reliability, especially under peak loads or when many notifications need to be sent simultaneously.

### 5. Advanced Monitoring and Alerts

The current monitoring setup via Prometheus and Grafana tracks request statuses and rate-limiting behavior. Future improvements could include:

- **Real-time Alerts**: Set up real-time alerts for when rate limits are being frequently exceeded or when the system is under heavy load.
- **Custom Metrics**: Add more granular metrics such as notification processing time, queue length, and worker health.

---

## Final Thoughts

This simple backend implementation serves as a functional prototype for notification rate-limiting. However, as traffic scales and more complex requirements arise, the system can evolve by integrating advanced features such as dynamic rate-limiting, on-demand workers, and notification prioritization.

As you move forward with the development of this system, consider the following questions:

1. **How will you handle different types of notifications with varying priority levels?**
2. **What monitoring and alerting mechanisms will be necessary to ensure the system remains healthy under heavy load?**
3. **How will you scale the system to handle spikes in traffic without sacrificing performance or reliability?**

By answering these questions, you can determine the future direction of the project and plan for its continued growth and improvement.

---

## References

- **Prometheus Documentation**: Learn more about setting up and configuring Prometheus for monitoring. [Prometheus Docs](https://prometheus.io/docs/)
- **Grafana Documentation**: Learn how to build custom dashboards and set up alerts in Grafana. [Grafana Docs](https://grafana.com/docs/)
- **AWS Lambda**: Official documentation for AWS Lambda for scaling and serverless functions. [AWS Lambda Docs](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html)

---


## Bottom line 

I was considering other scenarios, but another approach would be to implement this as a WAF rule within the WAF. 

The benefit of this approach is that it simplifies the setup of new applications by applying the rule. However, depending on the complexity of the rule, it could be vulnerable to attacks and may cause the WAF to fail 

if the rule isn’t optimized for performance or lacks protection for these scenarios
