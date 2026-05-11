# Sayyit.com
Like Reddit and X, but users are given left/right scores that allow them to be the only moderators.  
  
No hidden algorithms (or hidden elites) determinine what is seen.  
You can choose to see only left, only right, or a mix of both. A true town square.  
  
## Post Scoring
Posts are scored on a left/right scale based upon responses. If a post receives more left responses, it will be scored more left. If it receives more right responses, it will be scored more right.  
The equation for a post's score is:  
>  S<sub>n</sub> = **tanh**(k*m<sub>n</sub>)  * 100%
  
S<sub>n</sub> -> the score of a post after *n* responses, as a percentage.  
k -> a constant that determines how quickly the score changes with responses. Currently set to 0.1. 

When S<sub>n</sub> is negative the absolute value is displayed as a left score.  

The tanh function is used to ensure that the score remains between -1 and 1, where -1 represents a completely left post and 1 represents a completely right post.  
tanh ensures the score approaches -1 or 1 asymptotically, meaning that as more responses are received, the score will get closer and closer to -1 or 1, but will never actually reach those values. 
This allows for a more nuanced scoring system where posts can be strongly left or right without being completely one-sided.  

  
m<sub>n</sub> -> the mean score of the post after *n* replies. The mean score is calculated as:  
>  m<sub>n</sub> = (L + R) / (|L| + R)  

Where:  
 L -> the sum of all left responses. Each response is scored as -1.  
 R -> the number of all right responses. Each response is scored as +1.  

 The effect is a score that looks like:  
  ![tanh score shape](./images/tanh_score_shape.png)
